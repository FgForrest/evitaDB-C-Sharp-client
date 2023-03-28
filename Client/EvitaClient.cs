using System.Collections.Concurrent;
using Client.Certificate;
using Client.Config;
using Client.Converters.Models.Schema.Mutations;
using Client.Exceptions;
using Client.Interceptors;
using Client.Models.Schemas.Dtos;
using Client.Models.Schemas.Mutations;
using Client.Models.Schemas.Mutations.Catalog;
using Client.Pooling;
using Client.Session;
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;

namespace Client;

using EvitaDB;

public delegate void EvitaSessionTerminationCallback(EvitaClientSession session);

public class EvitaClient : IDisposable
{
    private static readonly ISchemaMutationConverter<ITopLevelCatalogSchemaMutation, GrpcTopLevelCatalogSchemaMutation>
        CatalogSchemaMutationConverter = new DelegatingTopLevelCatalogSchemaMutationConverter();

    private readonly ChannelPool _channelPool;

    private static int _active = 1;

    private readonly ConcurrentDictionary<Guid, EvitaClientSession> _activeSessions = new();
    private readonly ConcurrentDictionary<string, EvitaEntitySchemaCache> _entitySchemaCache = new();

    public EvitaClient(EvitaClientConfiguration configuration)
    {
        var certificateManager = new ClientCertificateManager.Builder()
            .SetClientCertificateFolderPath(configuration.CertificateFolderPath)
            .SetClientCertificatePath(configuration.CertificateKeyFileName)
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
    }

    private T ExecuteWithEvitaService<T>(Func<EvitaService.EvitaServiceClient, T> evitaServiceClient)
    {
        var channel = _channelPool.GetChannel();
        try
        {
            return evitaServiceClient.Invoke(new EvitaService.EvitaServiceClient(channel.Invoker));
        }
        finally
        {
            _channelPool.ReturnChannel(channel);
        }
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
            ? ExecuteWithEvitaService(evitaServiceClient => evitaServiceClient.CreateReadWriteSession(grpcRequest))
            : ExecuteWithEvitaService(evitaServiceClient => evitaServiceClient.CreateReadOnlySession(grpcRequest));
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
            ? await ExecuteWithEvitaService(async evitaServiceClient =>
                await evitaServiceClient.CreateReadWriteSessionAsync(grpcRequest))
            : await ExecuteWithEvitaService(async evitaServiceClient =>
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
            ExecuteWithEvitaService(evitaService=>evitaService.DeleteCatalogIfExists(request));
        bool success = grpcResponse.Success;
        if (success)
        {
            _entitySchemaCache.TryRemove(catalogName, out var result);
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
            ExecuteWithEvitaService(evitaService => evitaService.GetCatalogNames(new Empty()));
        return new HashSet<string>(
            grpcResponse.CatalogNames
        );
    }

    public async Task<ISet<string>> GetCatalogNamesAsync()
    {
        AssertActive();
        GrpcCatalogNamesResponse grpcResponse =
            await ExecuteWithEvitaService(async evitaService => await evitaService.GetCatalogNamesAsync(new Empty()));
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
        ExecuteWithEvitaService(evitaService => evitaService.Update(request));
    }

    public async Task UpdateAsync(params ITopLevelCatalogSchemaMutation[] catalogMutations)
    {
        AssertActive();

        List<GrpcTopLevelCatalogSchemaMutation> grpcSchemaMutations = catalogMutations
            .Select(CatalogSchemaMutationConverter.Convert)
            .ToList();

        GrpcUpdateEvitaRequest request = new GrpcUpdateEvitaRequest {SchemaMutations = {grpcSchemaMutations}};
        await ExecuteWithEvitaService(async evitaService => await evitaService.UpdateAsync(request));
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