using System.Globalization;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Session;
using Grpc.Net.Client.Web;
using static EvitaDB.Client.Queries.IQueryConstraints;

namespace EvitaDB.Storefront.Services;

/// <summary>
/// Owns the connection to evitaDB and everything discovered from the catalog itself: the supported locales and
/// currencies, the available price lists, which references are faceted and which attributes can carry a histogram.
///
/// Nothing here is hard-coded against the demo dataset - every value is read from the server, so the app keeps
/// working if the dataset changes.
/// </summary>
public sealed class EvitaCatalogContext : IAsyncDisposable
{
    private readonly EvitaStorefrontOptions _options;
    private EvitaClient? _client;
    private EvitaClientSession? _session;
    /// <summary>What initialization is currently doing - reported when it fails.</summary>
    private string? _step;

    public EvitaCatalogContext(EvitaStorefrontOptions options) => _options = options;

    /// <summary>Long-lived read-only session. Prefer <see cref="ExecuteAsync{T}"/> over using it directly.</summary>
    public EvitaClientSession Session =>
        _session ?? throw new InvalidOperationException("Catalog context has not been initialized yet.");

    /// <summary>
    /// Runs a query against the session, reopening it once if the server has dropped it meanwhile.
    ///
    /// evitaDB terminates sessions that have been idle for a while; the next call then fails with
    /// <see cref="InstanceTerminatedException"/> ("Evita session has been already terminated!"). For a page
    /// that a user leaves open this is normal, not an error, so it is recovered from rather than surfaced.
    ///
    /// Reopening is cheap: the schema cache lives on <see cref="EvitaClient"/> keyed by catalog, not on the
    /// session, so a fresh session inherits the primed schemas and no re-priming round trips happen.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<EvitaClientSession, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        EvitaClientSession session = Session;
        try
        {
            return await action(session).ConfigureAwait(false);
        }
        catch (InstanceTerminatedException)
        {
            await ReopenSessionAsync(session, cancellationToken).ConfigureAwait(false);
            return await action(Session).ConfigureAwait(false);
        }
    }

    /// <summary>In-flight reopen, shared by every query that noticed the expiry at the same time.</summary>
    private Task? _reopening;

    private Task ReopenSessionAsync(EvitaClientSession expired, CancellationToken cancellationToken)
    {
        // another call may have reopened it already while this one was in flight
        if (!ReferenceEquals(_session, expired))
        {
            return Task.CompletedTask;
        }
        if (_client is null)
        {
            throw new InvalidOperationException("Catalog context has not been initialized yet.");
        }
        // Queries are routinely overlapped (listing + category tree, the schema prime wave), so several of
        // them can trip over the same expired session at once - they must all await ONE replacement instead
        // of racing to open several sessions and leaking all but the last. A finished task is ignored rather
        // than cleared, which sidesteps the reset-ordering traps of a self-clearing field. Single-threaded
        // WASM guarantees the check and the assignment are not interleaved.
        if (_reopening is { IsCompleted: false })
        {
            return _reopening;
        }
        _reopening = ReopenCoreAsync(cancellationToken);
        return _reopening;

        async Task ReopenCoreAsync(CancellationToken ct)
        {
            _session = await _client!.CreateSessionAsync(
                new SessionTraits(_options.Catalog), ct
            ).ConfigureAwait(false);
        }
    }

    public IEntitySchema ProductSchema { get; private set; } = null!;

    /// <summary>Locales the Product collection is localized into - the demo dataset reports `cs`, `de`, `en`.</summary>
    public IReadOnlyList<CultureInfo> Locales { get; private set; } = [];

    /// <summary>Currencies prices exist in - the demo dataset reports `CZK` and `EUR`.</summary>
    public IReadOnlyList<Currency> Currencies { get; private set; } = [];

    /// <summary>Price list codes, ordered as returned by the server.</summary>
    public IReadOnlyList<string> PriceLists { get; private set; } = [];

    /// <summary>
    /// Names of the Product references that are faceted, i.e. the ones the facet panel may render. Driven by the
    /// schema rather than a hard-coded list - <see cref="IReferenceSchema.IsFaceted"/> already resolves the
    /// deprecated flat flag against the scoped one.
    /// </summary>
    public IReadOnlyList<string> FacetedReferences { get; private set; } = [];

    /// <summary>Numeric, filterable, non-localized Product attributes - candidates for an attribute histogram.</summary>
    public IReadOnlyList<string> HistogramAttributes { get; private set; } = [];

    /// <summary>Product attributes the catalog declares sortable - drives which sort options are offered.</summary>
    public IReadOnlyCollection<string> SortableAttributes { get; private set; } = [];

    /// <summary>Set when initialization failed, so the UI can render the reason instead of a blank page.</summary>
    public string? InitializationError { get; private set; }

    public bool IsReady => _session is not null && InitializationError is null;

    /// <summary>
    /// Connects, opens the session and primes the schema caches. Must complete before any query runs.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _step = "building the client configuration";
            // gRPC-Web is the only gRPC flavour a browser can speak - a plain GrpcChannel would try HTTP/2
            // directly and fail. GrpcWebHandler wraps the browser's fetch-backed HttpClientHandler.
            EvitaClientConfiguration configuration = new EvitaClientConfiguration.Builder()
                .SetClientId("evitaDB Blazor storefront demo")
                .SetHost(_options.Host)
                .SetPort(_options.Port)
                .SetTlsEnabled(_options.TlsEnabled)
                .SetUseGeneratedCertificate(false)
                .SetUsingTrustedRootCaCertificate(true)
                .SetHttpHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()))
                // the browser multiplexes for us; every extra channel is an extra HttpClient for nothing
                .SetChannelPoolSize(1)
                .Build();

            _step = $"creating the client for {_options.Host}:{_options.Port}";
            _client = await EvitaClient.Create(configuration).ConfigureAwait(false);

            // CreateSessionAsync, not CreateSession: the sync one issues a blocking gRPC call, which deadlocks
            // on WebAssembly's single thread.
            _step = $"opening a read-only session on catalog `{_options.Catalog}`";
            _session = await _client.CreateSessionAsync(
                new SessionTraits(_options.Catalog), cancellationToken
            ).ConfigureAwait(false);

            await PrimeSchemaCachesAsync(cancellationToken).ConfigureAwait(false);
            LoadCatalogMetadata();
            _step = null;
        }
        catch (Exception exception)
        {
            ErrorDetail.LogToConsole($"catalog initialization failed while {_step}", exception);
            InitializationError = $"While {_step}:\n{ErrorDetail.Describe(exception)}";
        }
    }

    /// <summary>
    /// Fetches the catalog schema and the schema of <b>every</b> entity type up front.
    ///
    /// This is not an optimization - it is what makes the driver usable in a browser at all. Converting a query
    /// response looks schemas up in <c>EvitaEntitySchemaCache</c> and, on a miss, calls a <i>blocking</i> fetcher.
    /// Priming every type (including those only reachable as referenced entities) guarantees the miss never
    /// happens. See documentation/architecture.md.
    ///
    /// Independent calls are deliberately overlapped instead of awaited one by one. Blazor WebAssembly is
    /// single-threaded, so this is interleaving, not multi-threading, and the session tolerates it: the session
    /// id travels in an <c>AsyncLocal</c> that flows with each call, the channel pool hands out extra channels
    /// on demand instead of blocking, and the schema-cache writes happen synchronously between awaits. Against
    /// a remote server every round trip costs real latency; the sequential version paid it once per entity
    /// type, this one pays it three times in total (schema+types wave, prime wave, price-list query chained
    /// onto the PriceList prime).
    /// </summary>
    private async Task PrimeSchemaCachesAsync(CancellationToken cancellationToken)
    {
        _step = "fetching the catalog schema and listing entity types";
        Task<ISealedCatalogSchema> catalogSchemaTask = Session.GetCatalogSchemaAsync(cancellationToken);
        Task<ISet<string>> entityTypesTask = Session.GetAllEntityTypesAsync(cancellationToken);
        await Task.WhenAll(catalogSchemaTask, entityTypesTask).ConfigureAwait(false);

        _step = "priming entity schemas and loading price lists";
        // the tasks below are complete after the WhenAll - awaiting them is synchronous unwrapping, used
        // instead of .Result, which the project convention bans outright as a WASM deadlock trap
        ISet<string> entityTypes = await entityTypesTask.ConfigureAwait(false);
        Dictionary<string, Task<ISealedEntitySchema?>> primes = entityTypes.ToDictionary(
            entityType => entityType,
            entityType => Session.GetEntitySchemaAsync(entityType, cancellationToken),
            StringComparer.Ordinal);

        // The price-list query only needs the PriceList schema (and the catalog schema, fetched above) to be
        // in the cache when its RESPONSE is converted - so it is chained onto that single prime rather than
        // onto the whole wave, and overlaps the remaining schema fetches. When the collection is absent the
        // plain query preserves the original failure mode.
        Task<IReadOnlyList<string>> priceListsTask =
            primes.TryGetValue(StorefrontSchema.PriceListCollection, out Task<ISealedEntitySchema?>? priceListPrime)
                ? ChainPriceListsAsync(priceListPrime, cancellationToken)
                : LoadPriceListsAsync(cancellationToken);

        await Task.WhenAll(primes.Values.Cast<Task>().Append(priceListsTask)).ConfigureAwait(false);
        PriceLists = await priceListsTask.ConfigureAwait(false);

        // the wave already fetched the Product schema - no second round trip for it
        _step = $"loading the `{StorefrontSchema.ProductCollection}` schema";
        ProductSchema =
            (primes.TryGetValue(StorefrontSchema.ProductCollection, out Task<ISealedEntitySchema?>? productPrime)
                ? await productPrime.ConfigureAwait(false)
                : null)
            ?? throw new CollectionNotFoundException(StorefrontSchema.ProductCollection);
    }

    private async Task<IReadOnlyList<string>> ChainPriceListsAsync(
        Task<ISealedEntitySchema?> priceListSchemaPrime, CancellationToken cancellationToken)
    {
        await priceListSchemaPrime.ConfigureAwait(false);
        return await LoadPriceListsAsync(cancellationToken).ConfigureAwait(false);
    }

    private void LoadCatalogMetadata()
    {
        Locales = ProductSchema.Locales.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
        Currencies = ProductSchema.Currencies.OrderBy(x => x.CurrencyCode, StringComparer.Ordinal).ToList();

        FacetedReferences = ProductSchema.References.Values
            .Where(reference => reference.IsFaceted)
            .Select(reference => reference.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        SortableAttributes = ProductSchema.Attributes.Values
            .Where(attribute => attribute.Sortable())
            .Select(attribute => attribute.Name)
            .ToHashSet(StringComparer.Ordinal);

        HistogramAttributes = ProductSchema.Attributes.Values
            .Where(attribute => attribute.Filterable() && !attribute.Localized() && IsNumeric(attribute.Type))
            .Select(attribute => attribute.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Price list codes come from the PriceList collection. `code` is a global (non-localized) attribute, so this
    /// query needs no locale.
    /// </summary>
    private async Task<IReadOnlyList<string>> LoadPriceListsAsync(CancellationToken cancellationToken)
    {
        EvitaEntityResponse response = await ExecuteAsync(session =>
            session.QueryAsync<EvitaEntityResponse, ISealedEntity>(
            Query(
                Collection(StorefrontSchema.PriceListCollection),
                Require(
                    Page(1, 100),
                    EntityFetch(AttributeContent(StorefrontSchema.CodeAttribute))
                )
            ),
            cancellationToken
        ), cancellationToken).ConfigureAwait(false);

        return response.RecordData
            .Select(entity => entity.GetAttribute(StorefrontSchema.CodeAttribute) as string)
            .Where(code => !string.IsNullOrEmpty(code))
            .Select(code => code!)
            .ToList();
    }

    private static bool IsNumeric(Type type)
    {
        Type effective = Nullable.GetUnderlyingType(type) ?? type;
        return effective == typeof(decimal) || effective == typeof(int) || effective == typeof(long)
               || effective == typeof(short) || effective == typeof(byte);
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            try
            {
                await _session.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // the session may already be gone server-side; nothing useful to do while tearing down
            }
            _session = null;
        }
        _client = null;
    }
}
