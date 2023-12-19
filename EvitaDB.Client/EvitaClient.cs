using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EvitaDB.Client.Cdc;
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
public partial class EvitaClient : IClientContext, IDisposable
{
    private static readonly ISchemaMutationConverter<ITopLevelCatalogSchemaMutation, GrpcTopLevelCatalogSchemaMutation>
        CatalogSchemaMutationConverter = new DelegatingTopLevelCatalogSchemaMutationConverter();

    private readonly ChannelPool? _channelPool;
    private readonly ChannelInvoker? _cdcChannel;

    private static int _active = 1;

    private readonly ConcurrentDictionary<Guid, EvitaClientSession> _activeSessions = new();
    private readonly ConcurrentDictionary<string, EvitaEntitySchemaCache> _entitySchemaCache = new();

    private readonly Action? _terminationCallback;

    public EvitaClientConfiguration Configuration { get; }

    private static readonly Regex ErrorMessagePattern = MyRegex();
    private EvitaClient(EvitaClientConfiguration configuration, ClientCertificateManager certificateManager)
    {
        Configuration = configuration;
        ChannelBuilder channelBuilder = new ChannelBuilder(
            Configuration.Host,
            Configuration.Port,
            certificateManager.BuildHttpClientHandler(),
            new ClientInterceptor(this)
        );
        _channelPool = new ChannelPool(channelBuilder, 10);
        _cdcChannel = channelBuilder.Build();

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

    public static async Task<EvitaClient> Create(EvitaClientConfiguration configuration)
    {
        ClientCertificateManager certificateManager = await new ClientCertificateManager.Builder()
            .SetClientCertificateFolderPath(configuration.CertificateFolderPath)
            .SetClientCertificatePath(configuration.CertificateFileName)
            .SetClientCertificateKeyPath(configuration.CertificateKeyFileName)
            .SetClientCertificateKeyPassword(configuration.CertificateKeyPassword)
            .SetUseGeneratedCertificate(configuration.UseGeneratedCertificate, configuration.Host,
                configuration.SystemApiPort)
            .SetTrustedServerCertificate(configuration.UsingTrustedRootCaCertificate)
            .Build();
        return new EvitaClient(configuration, certificateManager);
    }

    /// <summary>
    /// This method is used for registering a callback that is invoked any system event, like catalog creation or its
    /// top level mutation occurs.
    /// </summary>
    /// <param name="request">request for subscribing to system events</param>
    /// <returns>an observable collection that receives updates about changes in database</returns>
    public IObservable<ChangeSystemCapture> RegisterSystemChangeCapture(ChangeSystemCaptureRequest request)
    {
        return ExecuteWithStreamingEvitaService(stub =>
            stub.RegisterSystemChangeCapture(
                    new GrpcRegisterSystemChangeCaptureRequest
                    {
                        Content = EvitaEnumConverter.ToGrpcCaptureContent(request.Content)
                    }
                ).ResponseStream
                .AsObservable()
                .Select(x => ChangeDataCaptureConverter.ToChangeSystemCapture(x.Capture))
        );
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
        (this as IClientContext).ExecuteWithClientId(Configuration.ClientId, Close);
    }

    /// <summary>
    /// Returns complete listing of all catalogs known to the Evita instance.
    /// </summary>
    public ISet<string> GetCatalogNames()
    {
        AssertActive();
        GrpcCatalogNamesResponse grpcResponse =
            ExecuteWithBlockingEvitaService(evitaService => evitaService.GetCatalogNames(new Empty()));
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
        
        IClientContext context = this;
        return context.ExecuteWithClientId(Configuration.ClientId, () =>
        {
            if (!GetCatalogNames().Contains(catalogName))
            {
                Update(new CreateCatalogSchemaMutation(catalogName));
            }

            return QueryCatalog(catalogName, x => x.GetCatalogSchema(this)).OpenForWrite();
        });
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
        AssertActive();
        GrpcRenameCatalogRequest request = new GrpcRenameCatalogRequest
        {
            CatalogName = catalogName,
            NewCatalogName = newCatalogName
        };
        GrpcRenameCatalogResponse response =
            ExecuteWithBlockingEvitaService(evitaService => evitaService.RenameCatalog(request));
        if (response.Success)
        {
            _entitySchemaCache.Remove(catalogName, out _);
            _entitySchemaCache.Remove(newCatalogName, out _);
        }
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
        AssertActive();
        GrpcReplaceCatalogRequest request = new GrpcReplaceCatalogRequest
        {
            CatalogNameToBeReplacedWith = catalogNameToBeReplacedWith,
            CatalogNameToBeReplaced = catalogNameToBeReplaced
        };
        GrpcReplaceCatalogResponse response =
            ExecuteWithBlockingEvitaService(evitaService => evitaService.ReplaceCatalog(request));
        if (response.Success)
        {
            _entitySchemaCache.Remove(catalogNameToBeReplacedWith, out _);
            _entitySchemaCache.Remove(catalogNameToBeReplaced, out _);
        }
    }

    /// <summary>
    /// Deletes catalog with name `catalogName` along with its contents on disk.
    /// </summary>
    /// <param name="catalogName">name of the removed catalog</param>
    /// <returns>true if catalog was found in Evita and its contents were successfully removed</returns>
    public bool DeleteCatalogIfExists(string catalogName)
    {
        AssertActive();

        GrpcDeleteCatalogIfExistsRequest request = new GrpcDeleteCatalogIfExistsRequest
        {
            CatalogName = catalogName
        };
        GrpcDeleteCatalogIfExistsResponse grpcResponse =
            ExecuteWithBlockingEvitaService(evitaService => evitaService.DeleteCatalogIfExists(request));
        bool success = grpcResponse.Success;
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
        AssertActive();

        List<GrpcTopLevelCatalogSchemaMutation> grpcSchemaMutations = catalogMutations
            .Select(CatalogSchemaMutationConverter.Convert)
            .ToList();

        GrpcUpdateEvitaRequest request = new GrpcUpdateEvitaRequest {SchemaMutations = {grpcSchemaMutations}};
        ExecuteWithBlockingEvitaService(evitaService => evitaService.Update(request));
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
            IClientContext context = session;
            return context.ExecuteWithClientId(Configuration.ClientId, () => queryLogic.Invoke(session));
        }
        finally
        {
            session.Close();
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
            IClientContext context = session;
            context.ExecuteWithClientId(Configuration.ClientId, () => queryLogic.Invoke(session));
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
                ? new[] {SessionFlags.ReadWrite}
                : flags.Contains(SessionFlags.ReadWrite)
                    ? flags
                    : flags.Append(SessionFlags.ReadWrite).ToArray()
        );
        EvitaClientSession session = CreateSession(traits);
        try
        {
            IClientContext context = session;
            return context.ExecuteWithClientId(Configuration.ClientId, () => session.Execute(updater));
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
    public void UpdateCatalog(string catalogName, Action<EvitaClientSession> updater, params SessionFlags[]? flags)
    {
        UpdateCatalog(
            catalogName,
            evitaSession =>
            {
                IClientContext context = evitaSession;
                context.ExecuteWithClientId(Configuration.ClientId, () => updater.Invoke(evitaSession));
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
        if (Interlocked.CompareExchange(ref _active, 1, 0) == 1)
        {
            _activeSessions.Values.ToList().ForEach(session => session.Close());
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

    private T ExecuteWithBlockingEvitaService<T>(Func<EvitaService.EvitaServiceClient, T> logic)
    {
        return ExecuteWithEvitaService(
            new PooledChannelSupplier(_channelPool!),
            channel => new EvitaService.EvitaServiceClient(channel.Channel),
            logic
        );
    }

    private T ExecuteWithStreamingEvitaService<T>(Func<EvitaService.EvitaServiceClient, T> logic)
    {
        return ExecuteWithEvitaService(
            new SharedChannelSupplier(_cdcChannel!),
            channel => new EvitaService.EvitaServiceClient(channel.Channel),
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
    private T ExecuteWithEvitaService<TS, T>(IChannelSupplier channelSupplier, Func<ChannelInvoker, TS> stubBuilder,
        Func<TS, T> logic)
    {
        IClientContext context = this;
        return context.ExecuteWithClientAndRequestId(
            Configuration.ClientId,
            Guid.NewGuid().ToString(),
            () =>
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

                        throw new EvitaInternalError(description);
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
        );
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
        GrpcEvitaSessionRequest grpcRequest = new()
        {
            CatalogName = traits.CatalogName,
            DryRun = traits.IsDryRun(),
        };
        GrpcEvitaSessionResponse? grpcResponse = traits.IsReadWrite()
            ? ExecuteWithBlockingEvitaService(evitaServiceClient =>
                evitaServiceClient.CreateReadWriteSession(grpcRequest))
            : ExecuteWithBlockingEvitaService(evitaServiceClient =>
                evitaServiceClient.CreateReadOnlySession(grpcRequest));
        EvitaClientSession session = new EvitaClientSession(
            this,
            _entitySchemaCache.GetOrAdd(traits.CatalogName, new EvitaEntitySchemaCache(traits.CatalogName)),
            _channelPool!,
            traits.CatalogName,
            Enum.Parse<CatalogState>(grpcResponse.CatalogState.ToString()),
            Guid.Parse(grpcResponse.SessionId),
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
