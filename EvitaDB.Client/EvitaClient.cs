using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EvitaDB.Client.Certificate;
using EvitaDB.Client.Config;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Converters.Models;
using EvitaDB.Client.Converters.Models.Schema.Mutations;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Interceptors;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Client.Pooling;
using EvitaDB.Client.Services;
using EvitaDB.Client.Session;
using EvitaDB.Client.Utils;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Enum = System.Enum;
using StatusCode = Grpc.Core.StatusCode;

[assembly: InternalsVisibleTo("EvitaDB.Test")]

namespace EvitaDB.Client;

public delegate void EvitaSessionTerminationCallback(EvitaClientSession session);

/// <summary>
/// Evita is a specialized database with easy-to-use API for e-commerce systems. Purpose of this research is creating fast
/// and scalable engine that handles all complex tasks that e-commerce systems has to deal with on daily basis. Evita should
/// operate as a fast secondary lookup / search index used by application frontends. We aim for order of magnitude better
/// latency (10x faster or better) for common e-commerce tasks than other solutions based on SQL or NoSQL databases on the
/// same hardware specification. Evita should not be used for storing and handling primary data, and we don't aim for ACID
/// properties nor data corruption guarantees. Evita "index" must be treated as something that could be dropped any time and
/// built up from scratch easily again.
/// </summary>
public partial class EvitaClient : IDisposable
{
    private static readonly ISchemaMutationConverter<ITopLevelCatalogSchemaMutation, GrpcEngineMutation>
        CatalogSchemaMutationConverter = new DelegatingTopLevelCatalogSchemaMutationConverter();

    private readonly ChannelPool? _channelPool;
    private readonly ChannelInvoker? _cdcChannel;

    private int _active = 1;

    private readonly ConcurrentDictionary<Guid, EvitaClientSession> _activeSessions = new();
    private readonly ConcurrentDictionary<string, EvitaEntitySchemaCache> _entitySchemaCache = new();

    private readonly Action? _terminationCallback;
    
    private OpenTelemetrySetup? _openTelemetrySetup;

    public EvitaClientConfiguration Configuration { get; }

    /// <summary>
    /// Returns true when this client instance is active - i.e. it has not been closed yet.
    /// </summary>
    public bool IsActive => Volatile.Read(ref _active) == 1;

    private static readonly Regex ErrorMessagePattern = MyRegex();

    private EvitaClient(EvitaClientConfiguration configuration, ClientCertificateManager? certificateManager = null)
    {
        Configuration = configuration;
        HttpMessageHandler httpHandler;
        if (Configuration.HttpHandler is not null)
        {
            // caller-supplied transport (e.g. a gRPC-Web handler in Blazor WebAssembly) - used verbatim.
            // SocketsHttpHandler is not constructed at all: merely instantiating it throws on `browser-wasm`.
            // Keep-alive pings, idle timeouts and TLS are all owned by the supplied handler / the host.
            httpHandler = Configuration.HttpHandler;
        }
        else
        {
            SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            };
            if (Configuration.PingIntervalMilliseconds > 0)
            {
                // the evitaDB server (Armeria) closes idle connections - keep them alive by HTTP/2 pings
                socketsHttpHandler.KeepAlivePingDelay =
                    TimeSpan.FromMilliseconds(Configuration.PingIntervalMilliseconds);
                socketsHttpHandler.KeepAlivePingTimeout = TimeSpan.FromSeconds(10);
                socketsHttpHandler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;
            }
            if (Configuration.IdleTimeoutMilliseconds > 0)
            {
                socketsHttpHandler.PooledConnectionIdleTimeout =
                    TimeSpan.FromMilliseconds(Configuration.IdleTimeoutMilliseconds);
            }
            certificateManager?.ConfigureSslOptions(socketsHttpHandler);
            httpHandler = socketsHttpHandler;
        }
        ChannelBuilder channelBuilder = new ChannelBuilder(
            Configuration.Host,
            Configuration.Port,
            Configuration.TlsEnabled,
            httpHandler,
            new ClientInterceptor(configuration)
        );

        _channelPool = new ChannelPool(channelBuilder, Configuration.ChannelPoolSize);
        _cdcChannel = channelBuilder.Build();

        string? traceEndpointUrl = configuration.TraceEndpointUrl;
        if (traceEndpointUrl is not null)
        {
            _openTelemetrySetup = new OpenTelemetrySetup(configuration.TraceEndpointUrl!, configuration.TraceEndpointProtocol);
        }

        void TerminationCallback()
        {
            try
            {
                Assert.IsTrue(_channelPool.Shutdown(), () => new EvitaClientNotTerminatedException());
            }
            catch (Exception)
            {
                // terminated
                Thread.CurrentThread.Interrupt();
            }
        }

        _terminationCallback = TerminationCallback;
    }

    /// <summary>
    /// Initialize a new instance of <see cref="EvitaClient"/> with the specified configuration.
    /// </summary>
    /// <param name="configuration">configuration to be applied</param>
    /// <returns>newly created EvitaClient</returns>
    public static async Task<EvitaClient> Create(EvitaClientConfiguration configuration)
    {
        EvitaClient client;
        if (!configuration.TlsEnabled || configuration.HttpHandler is not null)
        {
            // a caller-supplied transport owns TLS itself - building a ClientCertificateManager here would
            // touch the file system and X509 APIs that are unavailable in a browser host
            client = new EvitaClient(configuration);
        }
        else
        {
            ClientCertificateManager certificateManager = await new ClientCertificateManager.Builder()
                .SetClientCertificateFolderPath(configuration.CertificateFolderPath)
                .SetClientCertificatePath(configuration.CertificateFileName)
                .SetClientCertificateKeyPath(configuration.CertificateKeyFileName)
                .SetClientCertificateKeyPassword(configuration.CertificateKeyPassword)
                .SetUseGeneratedCertificate(configuration.UseGeneratedCertificate, configuration.Host,
                    configuration.SystemApiPort)
                .SetTrustedServerCertificate(configuration.UsingTrustedRootCaCertificate)
                .SetUsingMtls(configuration.MtlsEnabled)
                .Build();
            client = new EvitaClient(configuration, certificateManager);
        }

        await client.VerifyServerCompatibilityAsync().ConfigureAwait(false);
        return client;
    }

    /// <summary>
    /// Verifies this client is not newer than the server it connects to - such a combination is unsupported
    /// because the client could rely on wire semantics the server does not provide yet.
    /// </summary>
    private async Task VerifyServerCompatibilityAsync()
    {
        Models.ServerStatus serverStatus;
        try
        {
            serverStatus = await Management().GetServerStatusAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // the compatibility check is best-effort - a server that cannot report its status yet (or a network
            // hiccup) must not prevent the client from being created
            return;
        }

        int[]? clientVersion = ParseVersion(Interceptors.ClientInterceptor.AdvertisedClientVersion);
        int[]? serverVersion = ParseVersion(serverStatus.Version);
        if (clientVersion is null || serverVersion is null)
        {
            // snapshot or unparseable versions - no verdict possible
            return;
        }

        for (int i = 0; i < Math.Min(clientVersion.Length, serverVersion.Length); i++)
        {
            if (clientVersion[i] < serverVersion[i])
            {
                return;
            }
            if (clientVersion[i] > serverVersion[i])
            {
                Close();
                throw new IncompatibleClientException(
                    $"This client speaks the evitaDB protocol version {Interceptors.ClientInterceptor.AdvertisedClientVersion}, " +
                    $"which is newer than the server version {serverStatus.Version}. Downgrade the client or upgrade the server."
                );
            }
        }
    }

    private static int[]? ParseVersion(string version)
    {
        if (version.Contains("SNAPSHOT", StringComparison.OrdinalIgnoreCase) || version == "?")
        {
            return null;
        }
        string[] parts = version.Split('-')[0].Split('.');
        int[] numbers = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out numbers[i]))
            {
                return null;
            }
        }
        return numbers;
    }

    /// <summary>
    /// Creates <see cref="EvitaClientSession"/> for querying and altering the database.
    /// Don't forget to <see cref="Close()"/> or <see cref="TerminateSession(EvitaClientSession)"/> when your work with Evita is finished.
    /// 
    /// EvitaClientSession is not thread safe!
    /// </summary>
    /// <param name="catalogName">name of the catalog on which the session should be created</param>
    /// <returns>created read-only session</returns>
    public EvitaClientSession CreateReadOnlySession(string catalogName)
    {
        return CreateSession(new SessionTraits(catalogName));
    }

    /// <summary>
    /// Creates <see cref="EvitaClientSession"/> for querying and altering the database.
    /// Don't forget to <see cref="Close()"/> or <see cref="TerminateSession(EvitaClientSession)"/> when your work with Evita is finished.
    /// 
    /// EvitaClientSession is not thread safe!
    /// </summary>
    /// <param name="catalogName">name of the catalog on which the session should be created</param>
    /// <returns>created read-write session</returns>
    public EvitaClientSession CreateReadWriteSession(string catalogName)
    {
        return CreateSession(new SessionTraits(catalogName, SessionFlags.ReadWrite));
    }

    /// <summary>
    /// Method returns active session by its unique id or NULL if such session is not found.
    /// </summary>
    /// <param name="catalogName">name of the catalog</param>
    /// <param name="sessionId">id of requested session</param>
    /// <returns>returns existing active session specified by params</returns>
    public EvitaClientSession? GetSessionById(string catalogName, Guid sessionId)
    {
        AssertActive();
        if (_activeSessions.TryGetValue(sessionId, out EvitaClientSession? session))
        {
            return session.CatalogName == catalogName ? session : null;
        }

        return null;
    }

    /// <summary>
    /// Terminates existing <see cref="EvitaClientSession"/>. When this method is called no additional calls to this EvitaSession
    /// is accepted and all will terminate with <see cref="InstanceTerminatedException"/>.
    /// </summary>
    public void TerminateSession(EvitaClientSession session)
    {
        AssertActive();
        session.Close();
    }

    /// <summary>
    /// Returns complete listing of all catalogs known to the Evita instance.
    /// </summary>
    public ISet<string> GetCatalogNames()
    {
        return GetCatalogNamesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetCatalogNames"/>.
    /// </summary>
    public async Task<ISet<string>> GetCatalogNamesAsync(CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcCatalogNamesResponse grpcResponse = await ExecuteWithBlockingEvitaServiceAsync(evitaService =>
            evitaService.GetCatalogNamesAsync(new Empty(), cancellationToken: cancellationToken).ResponseAsync
        ).ConfigureAwait(false);
        return new HashSet<string>(
            grpcResponse.CatalogNames
        );
    }

    /// <summary>
    /// Creates new catalog of particular name if it doesn't exist. The schema of the catalog (should it was created or
    /// not) is returned to the response.
    /// </summary>
    /// <param name="catalogName">name of the catalog</param>
    /// <returns>a builder for applying more catalog mutations</returns>
    public ICatalogSchemaBuilder DefineCatalog(string catalogName)
    {
        AssertActive();

        if (!GetCatalogNames().Contains(catalogName))
        {
            Update(new CreateCatalogSchemaMutation(catalogName));
        }

        return QueryCatalog(catalogName, x => x.GetCatalogSchema(this)).OpenForWrite();
    }

    /// <summary>
    /// Renames existing catalog to a new name. The `newCatalogName` must not clash with any existing catalog name,
    /// otherwise exception is thrown. If you need to rename catalog to a name of existing catalog use
    /// the <see cref="ReplaceCatalog(String, String)"/> method instead.
    /// 
    /// In case exception occurs the original catalog (`catalogName`) is guaranteed to be untouched,
    /// and the `newCatalogName` will not be present.
    /// </summary>
    /// <param name="catalogName">name of the catalog that will be renamed</param>
    /// <param name="newCatalogName">new name of the catalog</param>
    public void RenameCatalog(string catalogName, string newCatalogName)
    {
        RenameCatalogAsync(catalogName, newCatalogName).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="RenameCatalog"/>.
    /// </summary>
    public async Task RenameCatalogAsync(string catalogName, string newCatalogName,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcRenameCatalogRequest request = new GrpcRenameCatalogRequest
        {
            CatalogName = catalogName, NewCatalogName = newCatalogName
        };
        // renaming is a long-running engine operation - wait for it to reach 100%
        try
        {
            await ExecuteWithBlockingEvitaServiceAsync(async evitaService =>
            {
                using var call = evitaService.RenameCatalogWithProgress(request, cancellationToken: cancellationToken);
                return await DrainProgressStreamAsync(call, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (EvitaInternalError)
        {
            // the progress stream may be aborted by the server while the rename continues - fall back to
            // polling for the observable effect
            bool renamed = await WaitForCatalogNamesConditionAsync(
                names => names.Contains(newCatalogName) && !names.Contains(catalogName), cancellationToken
            ).ConfigureAwait(false);
            if (!renamed)
            {
                throw;
            }
        }
        _entitySchemaCache.Remove(catalogName, out _);
        _entitySchemaCache.Remove(newCatalogName, out _);
    }

    /// <summary>
    /// Replaces existing catalog of particular with the contents of the another catalog. When this method is
    /// successfully finished, the catalog `catalogNameToBeReplacedWith` will be known under the name of the
    /// `catalogNameToBeReplaced` and the original contents of the `catalogNameToBeReplaced` will be purged entirely.
    /// 
    /// In case exception occurs, the original catalog (`catalogNameToBeReplaced`) is guaranteed to be untouched, the
    /// state of `catalogNameToBeReplacedWith` is however unknown and should be treated as damaged.
    /// </summary>
    /// <param name="catalogNameToBeReplacedWith">name of the catalog that will become the successor of the original catalog (old name)</param>
    /// <param name="catalogNameToBeReplaced">name of the catalog that will be replaced and dropped (new name)</param>
    public void ReplaceCatalog(string catalogNameToBeReplacedWith, string catalogNameToBeReplaced)
    {
        ReplaceCatalogAsync(catalogNameToBeReplacedWith, catalogNameToBeReplaced).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="ReplaceCatalog"/>.
    /// </summary>
    public async Task ReplaceCatalogAsync(string catalogNameToBeReplacedWith, string catalogNameToBeReplaced,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcReplaceCatalogRequest request = new GrpcReplaceCatalogRequest
        {
            CatalogNameToBeReplacedWith = catalogNameToBeReplacedWith,
            CatalogNameToBeReplaced = catalogNameToBeReplaced
        };
        // replacing is a long-running engine operation - wait for it to reach 100%
        try
        {
            await ExecuteWithBlockingEvitaServiceAsync(async evitaService =>
            {
                using var call = evitaService.ReplaceCatalogWithProgress(request, cancellationToken: cancellationToken);
                return await DrainProgressStreamAsync(call, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (EvitaInternalError)
        {
            // the progress stream may be aborted by the server while the replace continues - fall back to
            // polling for the observable effect
            bool replaced = await WaitForCatalogNamesConditionAsync(
                names => !names.Contains(catalogNameToBeReplacedWith), cancellationToken
            ).ConfigureAwait(false);
            if (!replaced)
            {
                throw;
            }
        }
        _entitySchemaCache.Remove(catalogNameToBeReplacedWith, out _);
        _entitySchemaCache.Remove(catalogNameToBeReplaced, out _);
    }

    /// <summary>
    /// Deletes catalog with name `catalogName` along with its contents on disk.
    /// </summary>
    /// <param name="catalogName">name of the removed catalog</param>
    /// <returns>true if catalog was found in Evita and its contents were successfully removed</returns>
    public bool DeleteCatalogIfExists(string catalogName)
    {
        return DeleteCatalogIfExistsAsync(catalogName).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="DeleteCatalogIfExists"/>. Catalog removal is a long-running engine operation on
    /// the server - the call applies a `RemoveCatalogSchemaMutation` and waits for the operation to reach 100%.
    /// </summary>
    public async Task<bool> DeleteCatalogIfExistsAsync(string catalogName,
        CancellationToken cancellationToken = default)
    {
        AssertActive();

        // first close and remove all active sessions to particular catalog
        List<EvitaClientSession> activeCatalogSessions = _activeSessions.Values
            .Where(x => x.CatalogName == catalogName)
            .ToList();

        foreach (var session in activeCatalogSessions)
        {
            session.Dispose();
            _activeSessions.Remove(session.SessionId, out _);
        }

        ISet<string> catalogNames = await GetCatalogNamesAsync(cancellationToken).ConfigureAwait(false);
        if (!catalogNames.Contains(catalogName))
        {
            return false;
        }

        // then delete it and wait for the server-side operation to complete
        GrpcApplyMutationRequest request = new GrpcApplyMutationRequest
        {
            Mutation = new GrpcEngineMutation
            {
                RemoveCatalogSchemaMutation = new GrpcRemoveCatalogSchemaMutation { CatalogName = catalogName }
            }
        };
        bool success;
        try
        {
            success = await ExecuteWithBlockingEvitaServiceAsync(async evitaService =>
            {
                using var call = evitaService.ApplyMutationWithProgress(request, cancellationToken: cancellationToken);
                return await DrainProgressStreamAsync(call, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (EvitaInternalError)
        {
            // the progress stream may be aborted by the server while the removal continues - fall back to
            // polling for the observable effect
            success = await WaitForCatalogNamesConditionAsync(
                names => !names.Contains(catalogName), cancellationToken
            ).ConfigureAwait(false);
        }
        if (success)
        {
            _entitySchemaCache.Remove(catalogName, out _);
        }

        return success;
    }

    /// <summary>
    /// Applies catalog mutation affecting entire catalog.
    /// The reason why we use mutations for this is to be able to include those operations to the WAL that is
    /// synchronized to replicas.
    /// </summary>
    /// <param name="catalogMutations">an array of top level catalog schema mutations to be applied</param>
    public void Update(params ITopLevelCatalogSchemaMutation[] catalogMutations)
    {
        UpdateAsync(catalogMutations).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="Update"/>.
    /// </summary>
    public async Task UpdateAsync(ITopLevelCatalogSchemaMutation[] catalogMutations,
        CancellationToken cancellationToken = default)
    {
        AssertActive();

        // the original bulk `Update` RPC was replaced by per-mutation `ApplyMutation` on the engine level;
        // engine mutations are long-running operations - wait for each to reach 100%
        foreach (ITopLevelCatalogSchemaMutation catalogMutation in catalogMutations)
        {
            GrpcApplyMutationRequest request = new GrpcApplyMutationRequest
            {
                Mutation = CatalogSchemaMutationConverter.Convert(catalogMutation)
            };
            try
            {
                await ExecuteWithBlockingEvitaServiceAsync(async evitaService =>
                {
                    using var call = evitaService.ApplyMutationWithProgress(request, cancellationToken: cancellationToken);
                    return await DrainProgressStreamAsync(call, cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (EvitaInternalError)
            {
                // the progress stream may be aborted by the server while the mutation continues - fall back to
                // polling for the observable effect where one exists
                bool recovered = request.Mutation.MutationCase switch
                {
                    GrpcEngineMutation.MutationOneofCase.CreateCatalogSchemaMutation =>
                        await WaitForCatalogNamesConditionAsync(
                            names => names.Contains(request.Mutation.CreateCatalogSchemaMutation.CatalogName),
                            cancellationToken
                        ).ConfigureAwait(false),
                    GrpcEngineMutation.MutationOneofCase.RemoveCatalogSchemaMutation =>
                        await WaitForCatalogNamesConditionAsync(
                            names => !names.Contains(request.Mutation.RemoveCatalogSchemaMutation.CatalogName),
                            cancellationToken
                        ).ConfigureAwait(false),
                    _ => false
                };
                if (!recovered)
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Executes querying logic in the newly created Evita session. Session is safely closed at the end of this method
    /// and result is returned.
    /// Query logic is intended to be read-only. For read-write logic use <see cref="UpdateCatalog{T}"/> or
    /// open a transaction manually in the logic itself.
    /// 
    /// </summary>
    /// <param name="catalogName">name of catalog from which the data should be read</param>
    /// <param name="queryLogic">application logic that reads data</param>
    /// <param name="sessionFlags">flags for ad-hoc created session</param>
    public T QueryCatalog<T>(string catalogName, Func<EvitaClientSession, T> queryLogic,
        params SessionFlags[] sessionFlags)
    {
        AssertActive();
        EvitaClientSession session = CreateSession(new SessionTraits(catalogName, sessionFlags));
        try
        {
            return queryLogic.Invoke(session);
        }
        finally
        {
            session.Close();
        }
    }

    /// <summary>
    /// Async variant of <see cref="QueryCatalog{T}"/> - executes the async querying logic in an ad-hoc created
    /// session that is safely closed at the end of the call.
    /// </summary>
    public async Task<T> QueryCatalogAsync<T>(string catalogName, Func<EvitaClientSession, Task<T>> queryLogic,
        SessionFlags[]? sessionFlags = null, CancellationToken cancellationToken = default)
    {
        AssertActive();
        EvitaClientSession session = await CreateSessionAsync(
            new SessionTraits(catalogName, sessionFlags ?? []), cancellationToken
        ).ConfigureAwait(false);
        try
        {
            return await queryLogic.Invoke(session).ConfigureAwait(false);
        }
        finally
        {
            await session.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes querying logic in the newly created Evita session. Session is safely closed at the end of this method
    /// and result is returned.
    /// Query logic is intended to be read-only. For read-write logic use <see cref="UpdateCatalog{T}"/> or
    /// open a transaction manually in the logic itself.
    /// 
    /// </summary>
    /// <param name="catalogName">name of catalog from which the data should be read</param>
    /// <param name="queryLogic">application logic that reads data</param>
    /// <param name="sessionFlags">flags for ad-hoc created session</param>
    public void QueryCatalog(string catalogName, Action<EvitaClientSession> queryLogic,
        params SessionFlags[] sessionFlags)
    {
        AssertActive();
        EvitaClientSession session = CreateSession(new SessionTraits(catalogName, sessionFlags));
        try
        {
            queryLogic.Invoke(session);
        }
        finally
        {
            session.Close();
        }
    }

    /// <summary>
    /// Executes catalog read-write logic in the newly Evita session. When logic finishes without exception, changes are
    /// committed to the index, otherwise changes are roll-backed and no data is affected. Changes made by the updating
    /// logic are visible only within update function. Other threads outside the logic function work with non-changed
    /// data until transaction is committed to the index.
    /// Current version limitation:
    /// Only single updater can execute in parallel (i.e. updates are expected to be invoked by single thread in serial way).
    /// 
    /// </summary>
    /// <param name="catalogName">name of catalog upon which the changes should be executes</param>
    /// <param name="updater">application logic that reads and writes data</param>
    /// <param name="flags">flags for ad-hoc created session</param>
    public T UpdateCatalog<T>(string catalogName, Func<EvitaClientSession, T> updater, params SessionFlags[]? flags)
    {
        AssertActive();
        SessionTraits traits = new SessionTraits(
            catalogName,
            flags == null
                ? new[] { SessionFlags.ReadWrite }
                : flags.Contains(SessionFlags.ReadWrite)
                    ? flags
                    : flags.Append(SessionFlags.ReadWrite).ToArray()
        );
        EvitaClientSession session = CreateSession(traits);
        try
        {
            return session.Execute(updater);
        }
        finally
        {
            session.Close();
        }
    }

    /// <summary>
    /// Async variant of <see cref="UpdateCatalog{T}(string,Func{EvitaClientSession,T},SessionFlags[])"/> - executes
    /// the async read-write logic in an ad-hoc created transactional session that is safely closed at the end.
    /// </summary>
    public async Task<T> UpdateCatalogAsync<T>(string catalogName, Func<EvitaClientSession, Task<T>> updater,
        SessionFlags[]? flags = null, CancellationToken cancellationToken = default)
    {
        AssertActive();
        SessionTraits traits = new SessionTraits(
            catalogName,
            flags == null
                ? new[] { SessionFlags.ReadWrite }
                : flags.Contains(SessionFlags.ReadWrite)
                    ? flags
                    : flags.Append(SessionFlags.ReadWrite).ToArray()
        );
        EvitaClientSession session = CreateSession(traits);
        try
        {
            return await session.ExecuteAsync(updater).ConfigureAwait(false);
        }
        finally
        {
            await session.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes catalog read-write logic in the newly Evita session. When logic finishes without exception, changes are
    /// committed to the index, otherwise changes are roll-backed and no data is affected. Changes made by the updating
    /// logic are visible only within update function. Other threads outside the logic function work with non-changed
    /// data until transaction is committed to the index.
    /// Current version limitation:
    /// Only single updater can execute in parallel (i.e. updates are expected to be invoked by single thread in serial way).
    /// 
    /// </summary>
    /// <param name="catalogName">name of catalog upon which the changes should be executes</param>
    /// <param name="updater">application logic that reads and writes data</param>
    /// <param name="flags">flags for ad-hoc created session</param>
    public void UpdateCatalog(string catalogName, Action<EvitaClientSession> updater, params SessionFlags[]? flags)
    {
        UpdateCatalog(
            catalogName,
            evitaSession =>
            {
                updater.Invoke(evitaSession);
                return 0;
            },
            flags
        );
    }

    /// <summary>
    /// Closes currently opened sessions and shuts down the channel pool.
    /// </summary>
    public void Close()
    {
        if (Interlocked.CompareExchange(ref _active, 0, 1) == 1)
        {
            foreach (EvitaClientSession session in _activeSessions.Values.ToList())
            {
                try
                {
                    session.Close();
                }
                catch (EvitaInternalError)
                {
                    // the session may have been invalidated on the server side (e.g. its catalog was dropped or
                    // went live) - client shutdown must not fail because of it
                }
                catch (EvitaInvalidUsageException)
                {
                    // ditto
                }
            }
            _activeSessions.Clear();
            _channelPool?.Shutdown();
            _terminationCallback?.Invoke();
        }
    }

    /// <summary>
    /// Called automatically when <see cref="EvitaClient"/> instance is disposed.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    /// <summary>
    /// Consumes an engine-operation progress stream until it completes and returns whether the final (100%)
    /// message was observed. A stream error arriving after the operation already reported completion is ignored -
    /// the server may abort the stream spuriously (e.g. due to unrelated internal observer failures) even though
    /// the operation itself succeeded.
    /// </summary>
    private static async Task<bool> DrainProgressStreamAsync(
        AsyncServerStreamingCall<GrpcApplyMutationWithProgressResponse> call,
        CancellationToken cancellationToken)
    {
        bool completed = false;
        try
        {
            while (await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                if (call.ResponseStream.Current.ProgressInPercent >= 100)
                {
                    completed = true;
                }
            }
        }
        catch (RpcException) when (completed)
        {
            // the operation already reported completion - ignore the trailing stream error
        }
        return completed;
    }

    /// <summary>
    /// Polls the catalog name listing until the passed condition holds. Used as a fallback when an engine-operation
    /// progress stream is aborted by the server even though the operation itself keeps running.
    /// </summary>
    private async Task<bool> WaitForCatalogNamesConditionAsync(
        Func<ISet<string>, bool> condition,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < 240; i++)
        {
            ISet<string> names = await GetCatalogNamesAsync(cancellationToken).ConfigureAwait(false);
            if (condition(names))
            {
                return true;
            }
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
        return false;
    }

    /// <summary>
    /// Returns the current state of the named catalog or null when no such catalog exists.
    /// </summary>
    public CatalogState? GetCatalogState(string catalogName)
    {
        return GetCatalogStateAsync(catalogName).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetCatalogState"/>.
    /// </summary>
    public async Task<CatalogState?> GetCatalogStateAsync(string catalogName,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcGetCatalogStateResponse response = await ExecuteWithBlockingEvitaServiceAsync(evitaService =>
            evitaService.GetCatalogStateAsync(
                new GrpcGetCatalogStateRequest { CatalogName = catalogName },
                cancellationToken: cancellationToken
            ).ResponseAsync
        ).ConfigureAwait(false);
        return response.HasCatalogState ? EvitaEnumConverter.ToCatalogState(response.CatalogState) : null;
    }

    /// <summary>
    /// Activates an inactive catalog - i.e. loads it into memory so that sessions can be opened against it
    /// (e.g. after it was restored from a backup). Waits for the operation to complete.
    /// </summary>
    public void ActivateCatalog(string catalogName)
    {
        ActivateCatalogAsync(catalogName).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="ActivateCatalog"/>.
    /// </summary>
    public async Task ActivateCatalogAsync(string catalogName, CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcActivateCatalogRequest request = new GrpcActivateCatalogRequest { CatalogName = catalogName };
        try
        {
            await ExecuteWithBlockingEvitaServiceAsync(async evitaService =>
            {
                using var call = evitaService.ActivateCatalogWithProgress(request, cancellationToken: cancellationToken);
                return await DrainProgressStreamAsync(call, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (EvitaInternalError)
        {
            bool activated = await WaitForCatalogStateAsync(catalogName, CatalogState.Alive, cancellationToken)
                .ConfigureAwait(false);
            if (!activated)
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Deactivates a catalog - i.e. unloads it from memory. Waits for the operation to complete.
    /// </summary>
    public void DeactivateCatalog(string catalogName)
    {
        DeactivateCatalogAsync(catalogName).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="DeactivateCatalog"/>.
    /// </summary>
    public async Task DeactivateCatalogAsync(string catalogName, CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcDeactivateCatalogRequest request = new GrpcDeactivateCatalogRequest { CatalogName = catalogName };
        try
        {
            await ExecuteWithBlockingEvitaServiceAsync(async evitaService =>
            {
                using var call = evitaService.DeactivateCatalogWithProgress(request, cancellationToken: cancellationToken);
                return await DrainProgressStreamAsync(call, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (EvitaInternalError)
        {
            bool deactivated = await WaitForCatalogStateAsync(catalogName, CatalogState.Inactive, cancellationToken)
                .ConfigureAwait(false);
            if (!deactivated)
            {
                throw;
            }
        }
    }

    private async Task<bool> WaitForCatalogStateAsync(string catalogName, CatalogState expectedState,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < 240; i++)
        {
            CatalogState? state = await GetCatalogStateAsync(catalogName, cancellationToken).ConfigureAwait(false);
            if (state == expectedState)
            {
                return true;
            }
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
        return false;
    }

    /// <summary>
    /// Registers a system-level change capture subscription and returns the engine change events (catalog
    /// creations, removals, renames, ...) as an async stream. The stream stays open until cancelled - server
    /// heartbeats keep it alive. The first message from the server (the subscription acknowledgement) is
    /// consumed internally.
    /// </summary>
    public async IAsyncEnumerable<ChangeSystemCapture> RegisterSystemChangeCaptureAsync(
        ChangeSystemCaptureRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AssertActive();
        ChannelInvoker channel = new SharedChannelSupplier(_cdcChannel!).GetChannel();
        var service = new EvitaService.EvitaServiceClient(channel.Invoker);
        using var call = service.RegisterSystemChangeCapture(
            new GrpcRegisterSystemChangeCaptureRequest
            {
                Content = EvitaEnumConverter.ToGrpcCaptureContent(request.Content)
            },
            cancellationToken: cancellationToken
        );

        bool acknowledged = false;
        while (await MoveNextTranslated(call.ResponseStream, cancellationToken).ConfigureAwait(false))
        {
            GrpcRegisterSystemChangeCaptureResponse message = call.ResponseStream.Current;
            if (!acknowledged)
            {
                if (message.ResponseType != GrpcCaptureResponseType.Acknowledgement)
                {
                    throw new EvitaInternalError(
                        $"The first message of a change capture stream must be an acknowledgement, " +
                        $"but `{message.ResponseType}` was received!"
                    );
                }
                acknowledged = true;
                continue;
            }

            if (message.ResponseType == GrpcCaptureResponseType.Change)
            {
                yield return ChangeCaptureConverter.ToChangeSystemCapture(message.Capture);
            }
        }
    }

    private static async Task<bool> MoveNextTranslated<T>(IAsyncStreamReader<T> reader,
        CancellationToken cancellationToken)
    {
        try
        {
            return await reader.MoveNext(cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException rpcException)
        {
            throw TranslateRpcException(rpcException);
        }
    }

    private EvitaClientManagement? _management;

    /// <summary>
    /// Returns the management service of this evitaDB client instance - server status/configuration introspection,
    /// long-running task tracking and backup file handling.
    /// </summary>
    public EvitaClientManagement Management()
    {
        AssertActive();
        return _management ??= new EvitaClientManagement(this);
    }

    internal Task<T> ExecuteWithEvitaManagementServiceAsync<T>(
        Func<EvitaManagementService.EvitaManagementServiceClient, Task<T>> logic)
    {
        return ExecuteWithEvitaServiceAsync(
            new PooledChannelSupplier(_channelPool!),
            channel => new EvitaManagementService.EvitaManagementServiceClient(channel.Invoker),
            logic
        );
    }

    private Task<T> ExecuteWithBlockingEvitaServiceAsync<T>(Func<EvitaService.EvitaServiceClient, Task<T>> logic)
    {
        return ExecuteWithEvitaServiceAsync(
            new PooledChannelSupplier(_channelPool!),
            channel => new EvitaService.EvitaServiceClient(channel.Invoker),
            logic
        );
    }

    /// <summary>
    /// Async counterpart of <see cref="ExecuteWithEvitaService{TS,T}"/> - shares the channel handling and exception
    /// translation, but awaits the gRPC call instead of blocking.
    /// </summary>
    private async Task<T> ExecuteWithEvitaServiceAsync<TS, T>(
        IChannelSupplier channelSupplier,
        Func<ChannelInvoker, TS> stubBuilder,
        Func<TS, Task<T>> logic)
    {
        ChannelInvoker channel = channelSupplier.GetChannel();
        try
        {
            return await logic.Invoke(stubBuilder.Invoke(channel)).ConfigureAwait(false);
        }
        catch (RpcException rpcException)
        {
            throw TranslateRpcException(rpcException);
        }
        catch (EvitaInvalidUsageException)
        {
            throw;
        }
        catch (EvitaInternalError)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new EvitaInternalError(
                "Unexpected internal Evita error occurred: " + e.Message,
                "Unexpected internal Evita error occurred.",
                e
            );
        }
        finally
        {
            _channelPool?.ReleaseChannel(channel);
        }
    }

    /// <summary>
    /// Translates a gRPC failure to the corresponding evitaDB exception.
    /// </summary>
    private static Exception TranslateRpcException(RpcException rpcException)
    {
        StatusCode statusCode = rpcException.StatusCode;
        string description = rpcException.Status.Detail;
        Match expectedFormat = ErrorMessagePattern.Match(description);
        if (statusCode == StatusCode.InvalidArgument)
        {
            return expectedFormat.Success
                ? EvitaInvalidUsageException.CreateExceptionWithErrorCode(
                    expectedFormat.Groups[2].ToString(), expectedFormat.Groups[1].ToString()
                )
                : new EvitaInvalidUsageException(description);
        }

        return expectedFormat.Success
            ? EvitaInternalError.CreateExceptionWithErrorCode(
                expectedFormat.Groups[2].ToString(), expectedFormat.Groups[1].ToString()
            )
            : new EvitaInternalError(
                string.IsNullOrEmpty(description)
                    ? $"gRPC call failed with status `{statusCode}` and no error detail."
                    : description
            );
    }

    private T ExecuteWithBlockingEvitaService<T>(Func<EvitaService.EvitaServiceClient, T> logic)
    {
        return ExecuteWithEvitaService(
            new PooledChannelSupplier(_channelPool!),
            channel => new EvitaService.EvitaServiceClient(channel.Invoker),
            logic
        );
    }

    private T ExecuteWithStreamingEvitaService<T>(Func<EvitaService.EvitaServiceClient, T> logic)
    {
        return ExecuteWithEvitaService(
            new SharedChannelSupplier(_cdcChannel!),
            channel => new EvitaService.EvitaServiceClient(channel.Invoker),
            logic
        );
    }

    /// <summary>
    /// Method that is called within the <see cref="EvitaClientSession"/> to apply the wanted logic on a channel retrieved
    /// from a channel pool.
    /// </summary>
    /// <param name="channelSupplier">interface for retrieving a channel</param>
    /// <param name="stubBuilder">function that contains channel building logic</param>
    /// <param name="logic">logic to be executed on the created channel</param>
    /// <typeparam name="TS">channel type</typeparam>
    /// <typeparam name="T">response type</typeparam>
    /// <returns></returns>
    /// <exception cref="EvitaInvalidUsageException">thrown when error occurs by clients bad database manipulation</exception>
    /// <exception cref="EvitaInternalError">error cause by bad or unexpected behaviour on the database side</exception>
    private T ExecuteWithEvitaService<TS, T>(
        IChannelSupplier channelSupplier, 
        Func<ChannelInvoker, TS> stubBuilder,
        Func<TS, T> logic)
    {
        ChannelInvoker channel = channelSupplier.GetChannel();
        try
        {
            return logic.Invoke(stubBuilder.Invoke(channel));
        }
        catch (RpcException rpcException)
        {
            StatusCode statusCode = rpcException.StatusCode;
            string description = rpcException.Status.Detail;
            Match expectedFormat = ErrorMessagePattern.Match(description);
            if (statusCode == StatusCode.InvalidArgument)
            {
                if (expectedFormat.Success)
                {
                    throw EvitaInvalidUsageException.CreateExceptionWithErrorCode(
                        expectedFormat.Groups[2].ToString(), expectedFormat.Groups[1].ToString()
                    );
                }

                throw new EvitaInvalidUsageException(description);
            }
            else
            {
                if (expectedFormat.Success)
                {
                    throw EvitaInternalError.CreateExceptionWithErrorCode(
                        expectedFormat.Groups[2].ToString(), expectedFormat.Groups[1].ToString()
                    );
                }

                throw new EvitaInternalError(
                    string.IsNullOrEmpty(description)
                        ? $"gRPC call failed with status `{statusCode}` and no error detail."
                        : description
                );
            }
        }
        catch (EvitaInvalidUsageException)
        {
            throw;
        }
        catch (EvitaInternalError)
        {
            throw;
        }
        catch (Exception e)
        {
            Trace.TraceError($"Unexpected internal Evita error occurred: {e.Message}", e);
            throw new EvitaInternalError(
                "Unexpected internal Evita error occurred: " + e.Message,
                "Unexpected internal Evita error occurred.", e
            );
        }
        finally
        {
            channelSupplier.ReleaseChannel();
        }
    }

    /// <summary>
    /// Creates <see cref="EvitaClientSession"/> for querying the database. This is the most versatile method for initializing a new
    /// session allowing to pass all configurable options in `traits` argument.
    /// 
    /// Don't forget to <see cref="Close()"/> or <see cref="TerminateSession(EvitaClientSession)"/> when your work with Evita is finished.
    /// EvitaSession is not thread safe!
    /// </summary>
    /// <param name="traits">traits to customize the created session</param>
    /// <returns>new instance of EvitaSession</returns>
    public EvitaClientSession CreateSession(SessionTraits traits)
    {
        AssertActive();
        GrpcEvitaSessionRequest grpcRequest = new() { CatalogName = traits.CatalogName, DryRun = traits.IsDryRun(), };
        GrpcEvitaSessionResponse? grpcResponse = traits.IsReadWrite()
            ? ExecuteWithBlockingEvitaService(evitaServiceClient =>
                evitaServiceClient.CreateReadWriteSession(grpcRequest))
            : ExecuteWithBlockingEvitaService(evitaServiceClient =>
                evitaServiceClient.CreateReadOnlySession(grpcRequest));
        EvitaClientSession session = new EvitaClientSession(
            this,
            _entitySchemaCache.GetOrAdd(traits.CatalogName, new EvitaEntitySchemaCache(traits.CatalogName)),
            _channelPool!,
            _cdcChannel,
            traits.CatalogName,
            Enum.Parse<CatalogState>(grpcResponse.CatalogState.ToString()),
            Guid.Parse(grpcResponse.SessionId),
            Guid.Parse(grpcResponse.CatalogId),
            EvitaEnumConverter.ToCommitBehavior(grpcResponse.CommitBehaviour),
            traits,
            session =>
            {
                _activeSessions.Remove(session.SessionId, out _);
                traits.TerminationCallback?.Invoke(session);
            }
        );
        _activeSessions.TryAdd(session.SessionId, session);
        return session;
    }

    /// <summary>
    /// Async variant of <see cref="CreateSession"/>.
    ///
    /// Session creation is the entry point of every read path, so a host that cannot block - Blazor WebAssembly -
    /// must use this method; <see cref="CreateSession"/> issues a blocking gRPC call.
    /// </summary>
    /// <param name="traits">traits to customize the created session</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>new instance of EvitaSession</returns>
    public async Task<EvitaClientSession> CreateSessionAsync(SessionTraits traits,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcEvitaSessionRequest grpcRequest = new() { CatalogName = traits.CatalogName, DryRun = traits.IsDryRun(), };
        GrpcEvitaSessionResponse grpcResponse = traits.IsReadWrite()
            ? await ExecuteWithBlockingEvitaServiceAsync(evitaServiceClient =>
                evitaServiceClient.CreateReadWriteSessionAsync(grpcRequest, cancellationToken: cancellationToken)
                    .ResponseAsync).ConfigureAwait(false)
            : await ExecuteWithBlockingEvitaServiceAsync(evitaServiceClient =>
                evitaServiceClient.CreateReadOnlySessionAsync(grpcRequest, cancellationToken: cancellationToken)
                    .ResponseAsync).ConfigureAwait(false);
        EvitaClientSession session = new EvitaClientSession(
            this,
            _entitySchemaCache.GetOrAdd(traits.CatalogName, new EvitaEntitySchemaCache(traits.CatalogName)),
            _channelPool!,
            _cdcChannel,
            traits.CatalogName,
            Enum.Parse<CatalogState>(grpcResponse.CatalogState.ToString()),
            Guid.Parse(grpcResponse.SessionId),
            Guid.Parse(grpcResponse.CatalogId),
            EvitaEnumConverter.ToCommitBehavior(grpcResponse.CommitBehaviour),
            traits,
            session =>
            {
                _activeSessions.Remove(session.SessionId, out _);
                traits.TerminationCallback?.Invoke(session);
            }
        );
        _activeSessions.TryAdd(session.SessionId, session);
        return session;
    }

    /// <summary>
    /// Verifies this instance is still active.
    /// </summary>
    /// <exception cref="InstanceTerminatedException">thrown when client instance has already been terminated</exception>
    private void AssertActive()
    {
        if (_active == 0)
        {
            throw new InstanceTerminatedException("client instance");
        }
    }

    [GeneratedRegex(@"(\w+:\w+:\w+): (.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();
}
