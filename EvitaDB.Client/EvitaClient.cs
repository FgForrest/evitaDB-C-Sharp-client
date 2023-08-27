using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Client.Cdc;
using Client.Certificate;
using Client.Config;
using Client.Converters.DataTypes;
using Client.Converters.Models;
using Client.Converters.Models.Schema.Mutations;
using Client.Exceptions;
using Client.Interceptors;
using Client.Models.Cdc;
using Client.Models.Schemas.Dtos;
using Client.Models.Schemas.Mutations;
using Client.Models.Schemas.Mutations.Catalogs;
using Client.Pooling;
using Client.Services;
using Client.Session;
using EvitaDB;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Enum = System.Enum;

namespace Client;

public delegate void EvitaSessionTerminationCallback(EvitaClientSession session);

public class EvitaClient : IClientContext, IDisposable
{
    private static readonly ISchemaMutationConverter<ITopLevelCatalogSchemaMutation, GrpcTopLevelCatalogSchemaMutation>
        CatalogSchemaMutationConverter = new DelegatingTopLevelCatalogSchemaMutationConverter();

    private readonly ChannelPool _channelPool;
    private readonly ChannelInvoker _cdcChannel;

    private static int _active = 1;

    private readonly ConcurrentDictionary<Guid, EvitaClientSession> _activeSessions = new();
    private readonly ConcurrentDictionary<string, EvitaEntitySchemaCache> _entitySchemaCache = new();

    private readonly EvitaClientConfiguration _configuration;

    private static readonly Regex ErrorMessagePattern = new(@"(\w+:\w+:\w+): (.*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public EvitaClient(EvitaClientConfiguration configuration)
    {
        _configuration = configuration;
        var certificateManager = new ClientCertificateManager.Builder()
            .SetClientCertificateFolderPath(configuration.CertificateFolderPath)
            .SetClientCertificatePath(configuration.CertificateFileName)
            .SetClientCertificateKeyPath(configuration.CertificateKeyFileName)
            .SetClientCertificateKeyPassword(configuration.CertificateKeyPassword)
            .SetUseGeneratedCertificate(configuration.UseGeneratedCertificate)
            .SetTrustedServerCertificate(configuration.UsingTrustedRootCaCertificate)
            .Build();
        if (configuration.UseGeneratedCertificate)
        {
            certificateManager.GetCertificatesFromServer(configuration.Host, configuration.SystemApiPort);
        }

        var channelBuilder = new ChannelBuilder(
            configuration.Host,
            configuration.Port,
            certificateManager.BuildHttpClientHandler(),
            new ClientInterceptor()
        );
        _channelPool = new ChannelPool(channelBuilder, 10);
        _cdcChannel = channelBuilder.Build();
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
            _configuration.ClientId,
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

    public EvitaClientSession CreateReadOnlySession(string catalogName)
    {
        return CreateSession(new SessionTraits(catalogName));
    }

    public EvitaClientSession CreateReadWriteSession(string catalogName)
    {
        return CreateSession(new SessionTraits(catalogName, SessionFlags.ReadWrite));
    }

    private EvitaClientSession CreateSession(SessionTraits traits)
    {
        AssertActive();
        GrpcEvitaSessionRequest grpcRequest = new()
        {
            CatalogName = traits.CatalogName,
            DryRun = traits.IsDryRun(),
        };
        var grpcResponse = traits.IsReadWrite()
            ? ExecuteWithBlockingEvitaService(evitaServiceClient => evitaServiceClient.CreateReadWriteSession(grpcRequest))
            : ExecuteWithBlockingEvitaService(evitaServiceClient => evitaServiceClient.CreateReadOnlySession(grpcRequest));
        var session = new EvitaClientSession(
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

    private async Task<EvitaClientSession> CreateSessionAsync(SessionTraits traits)
    {
        AssertActive();
        GrpcEvitaSessionRequest grpcRequest = new()
        {
            CatalogName = traits.CatalogName,
            DryRun = traits.IsDryRun(),
        };
        var grpcResponse = traits.IsReadWrite()
            ? await ExecuteWithBlockingEvitaService(async evitaServiceClient =>
                await evitaServiceClient.CreateReadWriteSessionAsync(grpcRequest))
            : await ExecuteWithBlockingEvitaService(async evitaServiceClient =>
                await evitaServiceClient.CreateReadOnlySessionAsync(grpcRequest));
        var session = new EvitaClientSession(
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

    public void QueryCatalog(string catalogName, Action<EvitaClientSession> queryLogic,
        params SessionFlags[] sessionFlags)
    {
        AssertActive();
        using EvitaClientSession session = CreateSession(new SessionTraits(catalogName, sessionFlags));
        queryLogic.Invoke(session);
    }

    public async Task QueryCatalogAsync(string catalogName, Action<EvitaClientSession> queryLogic,
        params SessionFlags[] sessionFlags)
    {
        AssertActive();
        using EvitaClientSession session = await CreateSessionAsync(new SessionTraits(catalogName, sessionFlags));
        queryLogic.Invoke(session);
    }

    public T QueryCatalog<T>(string catalogName, Func<EvitaClientSession, T> queryLogic,
        params SessionFlags[] sessionFlags)
    {
        AssertActive();
        using EvitaClientSession session = CreateSession(new SessionTraits(catalogName, sessionFlags));
        return queryLogic.Invoke(session);
    }

    public async Task<T> QueryCatalogAsync<T>(string catalogName, Func<EvitaClientSession, T> queryLogic,
        params SessionFlags[] sessionFlags)
    {
        AssertActive();
        using EvitaClientSession session = await CreateSessionAsync(new SessionTraits(catalogName, sessionFlags));
        return queryLogic.Invoke(session);
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

    private void AssertActive()
    {
        if (_active == 0)
        {
            throw new InstanceTerminatedException("client instance");
        }
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

    public async Task<ISet<string>> GetCatalogNamesAsync()
    {
        AssertActive();
        GrpcCatalogNamesResponse grpcResponse =
            await ExecuteWithBlockingEvitaService(async evitaService => await evitaService.GetCatalogNamesAsync(new Empty()));
        return new HashSet<string>(
            grpcResponse.CatalogNames
        );
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

    public async Task UpdateAsync(params ITopLevelCatalogSchemaMutation[] catalogMutations)
    {
        AssertActive();

        List<GrpcTopLevelCatalogSchemaMutation> grpcSchemaMutations = catalogMutations
            .Select(CatalogSchemaMutationConverter.Convert)
            .ToList();

        GrpcUpdateEvitaRequest request = new GrpcUpdateEvitaRequest {SchemaMutations = {grpcSchemaMutations}};
        await ExecuteWithBlockingEvitaService(async evitaService => await evitaService.UpdateAsync(request));
    }

    public CatalogSchema DefineCatalog(string catalogName)
    {
        AssertActive();
        if (!GetCatalogNames().Contains(catalogName))
        {
            Update(new CreateCatalogSchemaMutation(catalogName));
        }

        return QueryCatalog(catalogName, x => x.GetCatalogSchema());
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

    public async Task<CatalogSchema> DefineCatalogAsync(string catalogName)
    {
        AssertActive();
        if (!(await GetCatalogNamesAsync()).Contains(catalogName))
        {
            await UpdateAsync(new CreateCatalogSchemaMutation(catalogName));
        }

        return await QueryCatalogAsync(catalogName, x => x.GetCatalogSchema());
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
}