using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
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
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Enum = System.Enum;

namespace EvitaDB.Client;

public delegate void EvitaSessionTerminationCallback(EvitaClientSession session);

public partial class EvitaClient : IClientContext, IDisposable
{
    private static readonly ISchemaMutationConverter<ITopLevelCatalogSchemaMutation, GrpcTopLevelCatalogSchemaMutation>
        CatalogSchemaMutationConverter = new DelegatingTopLevelCatalogSchemaMutationConverter();

    private readonly ChannelPool _channelPool;
    private readonly ChannelInvoker _cdcChannel;

    private static int _active = 1;

    private readonly ConcurrentDictionary<Guid, EvitaClientSession> _activeSessions = new();
    private readonly ConcurrentDictionary<string, EvitaEntitySchemaCache> _entitySchemaCache = new();

    public EvitaClientConfiguration Configuration { get; }

    private static readonly Regex ErrorMessagePattern = MyRegex();

    public EvitaClient(EvitaClientConfiguration configuration)
    {
        Configuration = configuration;
        ClientCertificateManager certificateManager = new ClientCertificateManager.Builder()
            .SetClientCertificateFolderPath(configuration.CertificateFolderPath)
            .SetClientCertificatePath(configuration.CertificateFileName)
            .SetClientCertificateKeyPath(configuration.CertificateKeyFileName)
            .SetClientCertificateKeyPassword(configuration.CertificateKeyPassword)
            .SetUseGeneratedCertificate(configuration.UseGeneratedCertificate, configuration.Host, configuration.SystemApiPort)
            .SetTrustedServerCertificate(configuration.UsingTrustedRootCaCertificate)
            .Build();

        ChannelBuilder channelBuilder = new ChannelBuilder(
            configuration.Host,
            configuration.Port,
            certificateManager.BuildHttpClientHandler(),
            new ClientInterceptor(this)
        );
        _channelPool = new ChannelPool(channelBuilder, 10);
        _cdcChannel = channelBuilder.Build();
    }

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

    public EvitaClientSession CreateReadOnlySession(string catalogName)
    {
        return CreateSession(new SessionTraits(catalogName));
    }

    public EvitaClientSession CreateReadWriteSession(string catalogName)
    {
        return CreateSession(new SessionTraits(catalogName, SessionFlags.ReadWrite));
    }

    public EvitaClientSession? GetSessionById(string catalogName, Guid sessionId)
    {
        AssertActive();
        if (_activeSessions.TryGetValue(sessionId, out EvitaClientSession? session))
        {
            return session.CatalogName == catalogName ? session : null;
        }

        return null;
    }

    public void TerminateSession(EvitaClientSession session)
    {
        AssertActive();
        session.Close();
    }

    public ISet<string> GetCatalogNames()
    {
        AssertActive();
        GrpcCatalogNamesResponse grpcResponse =
            ExecuteWithBlockingEvitaService(evitaService => evitaService.GetCatalogNames(new Empty()));
        return new HashSet<string>(
            grpcResponse.CatalogNames
        );
    }

    public ICatalogSchemaBuilder DefineCatalog(string catalogName)
    {
        AssertActive();
        if (!GetCatalogNames().Contains(catalogName))
        {
            Update(new CreateCatalogSchemaMutation(catalogName));
        }

        return QueryCatalog(catalogName, x => x.GetCatalogSchema()).OpenForWrite();
    }

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
            _entitySchemaCache.TryRemove(catalogName, out _);
        }

        return success;
    }

    public void Update(params ITopLevelCatalogSchemaMutation[] catalogMutations)
    {
        AssertActive();

        List<GrpcTopLevelCatalogSchemaMutation> grpcSchemaMutations = catalogMutations
            .Select(CatalogSchemaMutationConverter.Convert)
            .ToList();

        GrpcUpdateEvitaRequest request = new GrpcUpdateEvitaRequest {SchemaMutations = {grpcSchemaMutations}};
        ExecuteWithBlockingEvitaService(evitaService => evitaService.Update(request));
    }

    public T QueryCatalog<T>(string catalogName, Func<EvitaClientSession, T> queryLogic,
        params SessionFlags[] sessionFlags)
    {
        AssertActive();
        using EvitaClientSession session = CreateSession(new SessionTraits(catalogName, sessionFlags));
        return queryLogic.Invoke(session);
    }

    public void QueryCatalog(string catalogName, Action<EvitaClientSession> queryLogic,
        params SessionFlags[] sessionFlags)
    {
        AssertActive();
        using EvitaClientSession session = CreateSession(new SessionTraits(catalogName, sessionFlags));
        queryLogic.Invoke(session);
    }

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
        using EvitaClientSession session = CreateSession(traits);
        return session.Execute(updater);
    }

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
    
    public void Close()
    {
        if (Interlocked.CompareExchange(ref _active, 1, 0) == 1)
        {
            _activeSessions.Values.ToList().ForEach(session => session.CloseTransaction());
            _activeSessions.Clear();
            _channelPool.Shutdown();
        }
    }

    public void Dispose()
    {
        Close();
    }

    private T ExecuteWithBlockingEvitaService<T>(Func<EvitaService.EvitaServiceClient, T> logic)
    {
        return ExecuteWithEvitaService(
            new PooledChannelSupplier(_channelPool),
            channel => new EvitaService.EvitaServiceClient(channel.Channel),
            logic
        );
    }

    private T ExecuteWithStreamingEvitaService<T>(Func<EvitaService.EvitaServiceClient, T> logic)
    {
        return ExecuteWithEvitaService(
            new SharedChannelSupplier(_cdcChannel),
            channel => new EvitaService.EvitaServiceClient(channel.Channel),
            logic
        );
    }

    private T ExecuteWithEvitaService<TS, T>(IChannelSupplier channelSupplier, Func<ChannelInvoker, TS> stubBuilder,
        Func<TS, T> logic)
    {
        return (this as IClientContext).ExecuteWithClientAndRequestId(
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

    private EvitaClientSession CreateSession(SessionTraits traits)
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
            _entitySchemaCache.GetOrAdd(traits.CatalogName, new EvitaEntitySchemaCache(traits.CatalogName)),
            _channelPool,
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
        SessionIdHolder.SetSessionId(traits.CatalogName, grpcResponse.SessionId);
        return session;
    }

    private void AssertActive()
    {
        if (_active == 0)
        {
            throw new InstanceTerminatedException("client instance");
        }
    }

    [GeneratedRegex("(\\w+:\\w+:\\w+): (.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();
}