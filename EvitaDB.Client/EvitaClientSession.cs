using System.Text.RegularExpressions;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Converters.Models;
using EvitaDB.Client.Converters.Models.Data;
using EvitaDB.Client.Converters.Models.Data.Mutations;
using EvitaDB.Client.Converters.Models.Schema;
using EvitaDB.Client.Converters.Models.Schema.Mutations;
using EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;
using EvitaDB.Client.Converters.Queries;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Interceptors;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Client.Pooling;
using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Queries.Visitor;
using EvitaDB.Client.Services;
using EvitaDB.Client.Session;
using EvitaDB.Client.Utils;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using static EvitaDB.Client.Queries.Visitor.PrettyPrintingVisitor;
using static EvitaDB.Client.Queries.IQueryConstraints;
using StatusCode = Grpc.Core.StatusCode;

using TaskStatus = EvitaDB.Client.Models.TaskStatus;

namespace EvitaDB.Client;

/// <summary>
/// Session are created by the clients to envelope a "piece of work" with evitaDB. In web environment it's a good idea
/// to have session per request, in batch processing it's recommended to keep session per "record page" or "transaction".
/// There may be multiple <see cref="EvitaClientTransaction"/> during single session instance life but there is no support
/// for transactional overlap - there may be at most single transaction open in single session.
/// 
/// EvitaSession transactions behave like <a href="https://en.wikipedia.org/wiki/Snapshot_isolation">Snapshot</a>
/// transactions. When no transaction is explicitly opened - each query to Evita behaves as one small transaction. Data
/// updates are not allowed without explicitly opened transaction.
/// 
/// Don't forget to <see cref="Close()"/> when your work with Evita is finished.
/// EvitaSession contract is NOT thread safe.
/// </summary>
public partial class EvitaClientSession : IDisposable
{
    private static readonly ISchemaMutationConverter<ILocalCatalogSchemaMutation, GrpcLocalCatalogSchemaMutation>
        CatalogSchemaMutationConverter = new DelegatingLocalCatalogSchemaMutationConverter();

    private static readonly ISchemaMutationConverter<ModifyEntitySchemaMutation, GrpcModifyEntitySchemaMutation>
        ModifyEntitySchemaMutationConverter = new ModifyEntitySchemaMutationConverter();

    private static readonly IEntityMutationConverter<IEntityMutation, GrpcEntityMutation> EntityMutationConverter =
        new DelegatingEntityMutationConverter();

    private readonly ChannelPool _channelPool;
    private readonly ChannelInvoker? _cdcChannel;

    public EvitaClient Client { get; }
    private ClientEntitySchemaAccessor EntitySchemaAccessor { get; set; }
    public string CatalogName { get; }
    public CatalogState CatalogState { get; }
    public Guid SessionId { get; }
    public Guid CatalogId { get; }
    public EvitaClientTransaction.CommitBehavior CommitBehavior { get; }
    private readonly EvitaEntitySchemaCache _schemaCache;
    private readonly SessionTraits _sessionTraits;
    private readonly Action<EvitaClientSession> _onTerminationCallback;
    private readonly AtomicReference<EvitaClientTransaction> _transactionAccessor = new();

    private static readonly Regex ErrorMessagePattern = MyRegex();

    public bool Active { get; private set; } = true;
    private long _lastCall;

    private readonly string _clientId;

    public EvitaClientSession(
        EvitaClient evitaClient, EvitaEntitySchemaCache schemaCache, ChannelPool channelPool,
        ChannelInvoker? cdcChannel,
        string catalogName, CatalogState catalogState, Guid sessionId, Guid catalogId,
        EvitaClientTransaction.CommitBehavior commitBehavior, SessionTraits sessionTraits,
        Action<EvitaClientSession> onTerminationCallback)
    {
        _schemaCache = schemaCache;
        _channelPool = channelPool;
        _cdcChannel = cdcChannel;
        CatalogName = catalogName;
        CatalogState = catalogState;
        SessionId = sessionId;
        CatalogId = catalogId;
        CommitBehavior = commitBehavior;
        _sessionTraits = sessionTraits;
        _onTerminationCallback = onTerminationCallback;
        _clientId = evitaClient.Configuration.ClientId;
        Client = evitaClient;
        EntitySchemaAccessor = new ClientEntitySchemaAccessor(this);
    }

    /// <summary>
    /// Method creates new a new entity schema and collection for it in the catalog this session is tied to. It returns
    /// an <see cref="IEntitySchemaBuilder"/> that could be used for extending the initial "empty"
    /// <see cref="IEntitySchema"/>.
    /// 
    /// If the collection already exists the method returns a builder for entity schema of the already existing
    /// entity collection - i.e. this method behaves the same as calling:
    /// 
    /// GetEntitySchema("name")?.SealedEntitySchema.OpenForWrite()
    /// </summary>
    /// <param name="entityType">type of the collection to define</param>
    /// <returns>builder for applying more mutations on newly created entity schema</returns>
    public IEntitySchemaBuilder DefineEntitySchema(string entityType)
    {
        AssertActive();
        ISealedEntitySchema newEntitySchema = ExecuteInTransactionIfPossible(_ =>
        {
            var request = new GrpcDefineEntitySchemaRequest { EntityType = entityType };

            var response = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.DefineEntitySchema(request)
            );

            var theSchema = EntitySchemaConverter.Convert(response.EntitySchema);
            _schemaCache.SetLatestEntitySchema(theSchema);
            return new EntitySchemaDecorator(GetCatalogSchema, theSchema);
        });
        return newEntitySchema.OpenForWrite();
    }

    private T ExecuteWithBlockingEvitaSessionService<T>(Func<EvitaSessionService.EvitaSessionServiceClient, T> logic)
    {
        return ExecuteWithEvitaSessionService(
            new PooledChannelSupplier(_channelPool!),
            channel => new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker),
            logic
        );
    }

    private Task<T> ExecuteWithBlockingEvitaSessionServiceAsync<T>(
        Func<EvitaSessionService.EvitaSessionServiceClient, Task<T>> logic)
    {
        return ExecuteWithEvitaSessionServiceAsync(
            new PooledChannelSupplier(_channelPool!),
            channel => new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker),
            logic
        );
    }

    /// <summary>
    /// Async counterpart of <see cref="ExecuteWithEvitaSessionService{TS,T}"/> - shares the channel handling and
    /// exception translation, but awaits the gRPC call instead of blocking.
    /// </summary>
    private async Task<T> ExecuteWithEvitaSessionServiceAsync<TS, T>(
        IChannelSupplier channelSupplier,
        Func<ChannelInvoker, TS> stubBuilder,
        Func<TS, Task<T>> logic)
    {
        ChannelInvoker channel = channelSupplier.GetChannel();
        try
        {
            SessionIdHolder.SetSessionId(SessionId.ToString());
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
            _channelPool.ReleaseChannel(channel);
            SessionIdHolder.Reset();
        }
    }

    /// <summary>
    /// Translates a gRPC failure to the corresponding evitaDB exception. An UNAUTHENTICATED status means the server
    /// no longer recognizes this session - the session is closed locally as a side effect.
    /// </summary>
    private Exception TranslateRpcException(RpcException rpcException)
    {
        StatusCode statusCode = rpcException.StatusCode;
        string description = rpcException.Status.Detail;
        if (statusCode == StatusCode.Unauthenticated)
        {
            // close session and rethrow
            CloseInternally();
            return new InstanceTerminatedException("session");
        }

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

    /// <summary>
    ///  Method that is called within the <see cref="EvitaClientSession"/> to apply the wanted logic on a channel retrieved
    ///  from a channel pool.
    /// </summary>
    /// <param name="channelSupplier">interface for retrieving a channel</param>
    /// <param name="stubBuilder">function that contains channel building logic</param>
    /// <param name="logic">logic to be executed on the created channel</param>
    /// <typeparam name="TS">channel type</typeparam>
    /// <typeparam name="T">response type</typeparam>
    /// <returns>result of the applied function</returns>
    /// <exception cref="InstanceTerminatedException">thrown when no session has been passed to the server when one is required</exception>
    /// <exception cref="EvitaInvalidUsageException">error caused by invalid operations executed by the programmer</exception>
    /// <exception cref="EvitaInternalError">error caused by internal error in the database</exception>
    private T ExecuteWithEvitaSessionService<TS, T>(
        IChannelSupplier channelSupplier,
        Func<ChannelInvoker, TS> stubBuilder,
        Func<TS, T> logic)
    {
        ChannelInvoker channel = channelSupplier.GetChannel();
        try
        {
            SessionIdHolder.SetSessionId(SessionId.ToString());
            return logic.Invoke(stubBuilder.Invoke(channel));
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
            _channelPool.ReleaseChannel(channel);
            SessionIdHolder.Reset();
        }
    }

    /// <summary>
    /// If <see cref="ICatalog"/> supports transactions <see cref="ICatalog.SupportsTransaction"/> method
    /// executes application `logic` in current session and commits the transaction at the end. Transaction is
    /// automatically roll-backed when exception is thrown from the `logic` scope. Changes made by the updating logic are
    /// visible only within update function. Other threads outside the logic function work with non-changed data until
    /// transaction is committed to the index.
    /// 
    /// When catalog doesn't support transactions application `logic` is immediately applied to the index data and logic
    /// operates in a <a href="https://en.wikipedia.org/wiki/Isolation_(database_systems)#Read_uncommitted">read
    /// uncommitted</a> mode. Application `logic` can only append new entities in non-transactional mode.
    /// </summary>
    /// <param name="logic">logic to execute</param>
    /// <typeparam name="T">return type</typeparam>
    /// <returns>result of logic that possibly has been executed in transaction</returns>
    public T Execute<T>(Func<EvitaClientSession, T> logic)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(logic);
    }

    /// <summary>
    /// Async variant of <see cref="Execute{T}"/> - the logic runs within a server transaction when the catalog
    /// supports transactions.
    /// </summary>
    public Task<T> ExecuteAsync<T>(Func<EvitaClientSession, Task<T>> logic)
    {
        AssertActive();
        return ExecuteInTransactionIfPossibleAsync(logic);
    }

    /// <summary>
    /// If <see cref="ICatalog"/> supports transactions <see cref="ICatalog.SupportsTransaction"/> method
    /// executes application `logic` in current session and commits the transaction at the end. Transaction is
    /// automatically roll-backed when exception is thrown from the `logic` scope. Changes made by the updating logic are
    /// visible only within update function. Other threads outside the logic function work with non-changed data until
    /// transaction is committed to the index.
    /// 
    /// When catalog doesn't support transactions application `logic` is immediately applied to the index data and logic
    /// operates in a <a href="https://en.wikipedia.org/wiki/Isolation_(database_systems)#Read_uncommitted">read
    /// uncommitted</a> mode. Application `logic` can only append new entities in non-transactional mode.
    /// </summary>
    /// <param name="logic">logic to execute</param>
    /// <returns>result of logic that possibly has been executed in transaction</returns>
    public void Execute(Action<EvitaClientSession> logic)
    {
        AssertActive();
        ExecuteInTransactionIfPossible(
            evitaSessionContract =>
            {
                logic.Invoke(evitaSessionContract);
                return 0;
            }
        );
    }

    /// <summary>
    /// Returns list of all entity types available in this catalog.
    /// </summary>
    public ISet<string> GetAllEntityTypes()
    {
        return GetAllEntityTypesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetAllEntityTypes"/>.
    /// </summary>
    public async Task<ISet<string>> GetAllEntityTypesAsync(CancellationToken cancellationToken = default)
    {
        AssertActive();
        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
            evitaSessionService.GetAllEntityTypesAsync(new Empty(), cancellationToken: cancellationToken).ResponseAsync
        ).ConfigureAwait(false);
        return new HashSet<string>(grpcResponse.EntityTypes);
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> and returns zero or exactly one entity result. Method
    /// behaves exactly the same as <see cref="Query{T, TS}(Query)"/> but verifies the count of returned results and
    /// translates it to simplified return type.
    /// 
    /// Because result is generic and may contain different data as its contents (based on input query), additional
    /// parameter `expectedType` is passed. This parameter allows to check whether passed response contains the expected
    /// type of data before returning it back to the client. This should prevent late ClassCastExceptions on the client
    /// side.
    /// </summary>
    /// <param name="query">query to process</param>
    /// <typeparam name="TS">type of classifier that should be returned from the method call</typeparam>
    /// <returns>a computed response</returns>
    /// <exception cref="EvitaInvalidUsageException">thrown when invalid query was passed</exception>
    public TS? QueryOne<TS>(Query query) where TS : class, IEntityClassifier
    {
        return QueryOneAsync<TS>(query).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="QueryOne{TS}"/>.
    /// </summary>
    public async Task<TS?> QueryOneAsync<TS>(Query query, CancellationToken cancellationToken = default)
        where TS : class, IEntityClassifier
    {
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        var stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = { stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam) }
        };
        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(session =>
            session.QueryOneAsync(request, cancellationToken: cancellationToken).ResponseAsync
        ).ConfigureAwait(false);

        if (typeof(IEntityReference).IsAssignableFrom(typeof(TS)))
        {
            return (grpcResponse.EntityReference is not null
                ? EntityConverter.ToEntityReference(grpcResponse.EntityReference)
                : null) as TS;
        }

        if (typeof(ISealedEntity).IsAssignableFrom(typeof(TS)))
        {
            return grpcResponse.SealedEntity is not null
                ? EntityConverter.ToEntity<TS>(
                    entity => _schemaCache.GetEntitySchemaOrThrow(
                        entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                    ),
                    grpcResponse.SealedEntity,
                    new EvitaRequest(
                        query,
                        DateTimeOffset.Now
                    )
                )
                : null;
        }

        throw new EvitaInvalidUsageException("Unsupported return type `" + typeof(TS) + "`!");
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> and returns simplified list of results. Method
    /// behaves exactly the same as  but verifies the count of returned results and
    /// translates it to simplified return type. This method will throw out all possible extra results from, because there is
    /// no way how to propagate them in return value. If you require extra results or paginated list use
    /// the <see cref="Query{T, TS}(Query)"/> method.
    /// 
    /// Because result is generic and may contain different data as its contents (based on input query), additional
    /// parameter `expectedType` is passed. This parameter allows to check whether passed response contains the expected
    /// type of data before returning it back to the client. This should prevent late ClassCastExceptions on the client
    /// side.
    /// </summary>
    /// <param name="query">query to process</param>
    /// <typeparam name="TS">type of classifier that should be returned from the method call</typeparam>
    /// <returns>a computed response</returns>
    /// <exception cref="EvitaInvalidUsageException">thrown when invalid query was passed</exception>
    public IList<TS> QueryList<TS>(Query query) where TS : IEntityClassifier
    {
        return QueryListAsync<TS>(query).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="QueryList{TS}"/>.
    /// </summary>
    public async Task<IList<TS>> QueryListAsync<TS>(Query query, CancellationToken cancellationToken = default)
        where TS : IEntityClassifier
    {
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        var stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = { stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam) }
        };

        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(session =>
            session.QueryListAsync(request, cancellationToken: cancellationToken).ResponseAsync
        ).ConfigureAwait(false);

        if (typeof(IEntityReference).IsAssignableFrom(typeof(TS)))
        {
            return (IList<TS>)EntityConverter.ToEntityReferences(grpcResponse.EntityReferences);
        }

        if (typeof(ISealedEntity).IsAssignableFrom(typeof(TS)))
        {
            return EntityConverter.ToEntities<TS>(
                grpcResponse.SealedEntities,
                (entityType, schemaVersion) => _schemaCache.GetEntitySchemaOrThrow(
                    entityType, schemaVersion, FetchEntitySchema, GetCatalogSchema
                ),
                new EvitaRequest(
                    query,
                    DateTimeOffset.Now
                )
            );
        }

        throw new EvitaInvalidUsageException("Unsupported return type `" + typeof(TS) + "`!");
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> data and returns result. Because result is generic and may contain
    /// different data as its contents (based on input query), additional parameter `expectedType` is passed. This parameter
    /// allows to check whether passed response contains the expected type of data before returning it back to the client.
    /// This should prevent late ClassCastExceptions on the client side.
    /// </summary>
    /// <param name="query">query to process</param>
    /// <typeparam name="T">requested response type</typeparam>
    /// <typeparam name="TS">expected type of returned entities</typeparam>
    /// <returns>a requested result type</returns>
    /// <exception cref="EvitaInvalidUsageException">thrown when invalid query was passed</exception>
    /// <seealso cref="IQueryConstraints"/>
    public T Query<T, TS>(Query query) where TS : IEntityClassifier where T : EvitaResponse<TS>
    {
        return QueryAsync<T, TS>(query).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="Query{T,TS}"/>.
    /// </summary>
    public async Task<T> QueryAsync<T, TS>(Query query, CancellationToken cancellationToken = default)
        where TS : IEntityClassifier where T : EvitaResponse<TS>
    {
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        var stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = { stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam) }
        };
        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(session =>
            session.QueryAsync(request, cancellationToken: cancellationToken).ResponseAsync
        ).ConfigureAwait(false);
        var extraResults = GetEvitaResponseExtraResults(
            grpcResponse,
            new EvitaRequest(query, DateTimeOffset.Now)
        );

        if (typeof(IEntityReference).IsAssignableFrom(typeof(TS)))
        {
            var recordPage = ResponseConverter.ConvertToDataChunk(
                grpcResponse,
                grpcRecordPage => EntityConverter.ToEntityReferences(grpcRecordPage.EntityReferences)
            );
            return (new EvitaEntityReferenceResponse(query, recordPage, extraResults) as T)!;
        }

        if (typeof(ISealedEntity).IsAssignableFrom(typeof(TS)))
        {
            var recordPage = ResponseConverter.ConvertToDataChunk(
                grpcResponse,
                grpcRecordPage => EntityConverter.ToEntities<ISealedEntity>(
                    grpcRecordPage.SealedEntities.ToList(),
                    (entityType, schemaVersion) => _schemaCache.GetEntitySchemaOrThrow(
                        entityType, schemaVersion, FetchEntitySchema, GetCatalogSchema
                    ),
                    new EvitaRequest(
                        query,
                        DateTimeOffset.Now
                    )
                )
            );
            return (new EvitaEntityResponse(query, recordPage, extraResults) as T)!;
        }

        throw new EvitaInvalidUsageException("Unsupported return type `" + typeof(TS) + "`!");
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> data and returns result.
    /// </summary>
    /// <param name="query">input query,
    /// for creation use <see cref="Query"/> or similar methods
    /// for defining constraint use {@link QueryConstraints} static methods</param>
    /// <returns>full response data transfer object with all available data</returns>
    /// <seealso cref="IQueryConstraints"/>
    public EvitaResponse<ISealedEntity> QuerySealedEntity(Query query)
    {
        return Query<EvitaEntityResponse, ISealedEntity>(EnsureEntityFetchPresent(query));
    }

    /// <summary>
    /// Async variant of <see cref="QuerySealedEntity"/>.
    /// </summary>
    public async Task<EvitaResponse<ISealedEntity>> QuerySealedEntityAsync(Query query,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync<EvitaEntityResponse, ISealedEntity>(
            EnsureEntityFetchPresent(query), cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the query enriched by an `entityFetch` requirement when none is present, so that full entity bodies
    /// are returned.
    /// </summary>
    private static Query EnsureEntityFetchPresent(Query query)
    {
        if (query.Require == null)
        {
            return IQueryConstraints.Query(
                query.Collection,
                query.FilterBy,
                query.OrderBy,
                Require(EntityFetch())
            );
        }

        if (FinderVisitor.FindConstraints<IConstraint>(query.Require, x => x is EntityFetch,
                x => x is ISeparateEntityContentRequireContainer).Count == 0)
        {
            return IQueryConstraints.Query(
                query.Collection,
                query.FilterBy,
                query.OrderBy,
                (Require)query.Require.GetCopyWithNewChildren(
                    new IRequireConstraint?[] { Require(EntityFetch()) }
                        .Concat(query.Require.Children).ToArray(),
                    query.Require.AdditionalChildren
                )
            );
        }

        return query;
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> data and returns result.
    /// </summary>
    /// <param name="query">input query,
    /// for creation use <see cref="Query"/> or similar methods
    /// for defining constraint use {@link QueryConstraints} static methods</param>
    /// <returns>response data transfer object only primary keys and and entity types included</returns>
    /// <seealso cref="IQueryConstraints"/>
    public EvitaResponse<EntityReference> QueryEntityReference(Query query)
    {
        return Query<EvitaEntityReferenceResponse, EntityReference>(query);
    }

    /// <summary>
    /// Async variant of <see cref="QueryEntityReference"/>.
    /// </summary>
    public async Task<EvitaResponse<EntityReference>> QueryEntityReferenceAsync(Query query,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync<EvitaEntityReferenceResponse, EntityReference>(query, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Method alters one of the <see cref="IEntitySchema"/> of the catalog this session is tied to. All
    /// mutations will be applied or none of them (method call is atomic). The method call is idempotent - it means that
    /// when the method is called multiple times with same mutations the changes occur only once.
    /// </summary>
    /// <param name="entitySchemaBuilder">the builder that contains the mutations in the entity schema</param>
    /// <returns>possibly updated body of the <see cref="IEntitySchema"/> or the original schema if no change occurred</returns>
    public ISealedEntitySchema UpdateAndFetchEntitySchema(IEntitySchemaBuilder entitySchemaBuilder)
    {
        var schemaMutation = entitySchemaBuilder.ToMutation();
        if (schemaMutation is not null)
        {
            return UpdateAndFetchEntitySchema(schemaMutation);
        }

        return GetEntitySchemaOrThrow(entitySchemaBuilder.Name);
    }

    /// <summary>
    /// This internal method will physically call over the network and fetch actual {@link EntitySchema}.
    /// </summary>
    private EntitySchema? FetchEntitySchema(string entityType)
    {
        var grpcResponse = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetEntitySchema(new GrpcEntitySchemaRequest { EntityType = entityType })
        );
        if (grpcResponse.EntitySchema is null)
        {
            return null;
        }

        return EntitySchemaConverter.Convert(grpcResponse.EntitySchema);
    }

    /// <summary>
    /// Method returns entity by its type and primary key in requested form of completeness. This method allows quick
    /// access to the entity contents when primary key is known.
    /// </summary>
    public ISealedEntity? GetEntity(string entityType, int primaryKey,
        params IEntityContentRequire[] require)
    {
        return GetEntityAsync(entityType, primaryKey, require).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetEntity"/>.
    /// </summary>
    public async Task<ISealedEntity?> GetEntityAsync(string entityType, int primaryKey,
        IEntityContentRequire[] require, CancellationToken cancellationToken = default)
    {
        AssertActive();

        var stringWithParameters = ToStringWithParameterExtraction(require);
        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
            evitaSessionService.GetEntityAsync(
                new GrpcEntityRequest
                {
                    EntityType = entityType,
                    PrimaryKey = primaryKey,
                    Require = stringWithParameters.Query,
                    PositionalQueryParams =
                    {
                        stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)
                    }
                },
                cancellationToken: cancellationToken
            ).ResponseAsync
        ).ConfigureAwait(false);

        return grpcResponse.Entity is not null
            ? EntityConverter.ToEntity<ISealedEntity>(
                entity => _schemaCache.GetEntitySchemaOrThrow(
                    entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                ),
                grpcResponse.Entity,
                new EvitaRequest(
                    IQueryConstraints.Query(
                        Collection(entityType),
                        Require(
                            EntityFetch(require)
                        )
                    ),
                    DateTimeOffset.Now
                )
            )
            : null;
    }

    /// <summary>
    /// Method returns count of all entities stored in the collection of passed entity type.
    /// </summary>
    public int GetEntityCollectionSize(string entityType)
    {
        return GetEntityCollectionSizeAsync(entityType).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetEntityCollectionSize"/>.
    /// </summary>
    public async Task<int> GetEntityCollectionSizeAsync(string entityType,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
            evitaSessionService.GetEntityCollectionSizeAsync(
                new GrpcEntityCollectionSizeRequest { EntityType = entityType },
                cancellationToken: cancellationToken
            ).ResponseAsync
        ).ConfigureAwait(false);
        return grpcResponse.Size;
    }

    /// <summary>
    /// Method alters the <see cref="ICatalogSchema"/> of the catalog this session is tied to. The method is equivalent
    /// to <see cref="UpdateCatalogSchema(EvitaDB.Client.Models.Schemas.Mutations.ILocalCatalogSchemaMutation[])"/> but accepts the original builder. This method variant
    /// is present as a shortcut option for the developers.
    /// </summary>
    /// <param name="schemaMutation">the builder that contains the mutations in the catalog schema</param>
    /// <returns>version of the altered schema or current version if no modification occurred.</returns>
    public int UpdateCatalogSchema(params ILocalCatalogSchemaMutation[] schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var
                grpcSchemaMutations = schemaMutation
                    .Select(CatalogSchemaMutationConverter.Convert)
                    .ToList();

            var request = new GrpcUpdateCatalogSchemaRequest { SchemaMutations = { grpcSchemaMutations } };

            var response = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateCatalogSchema(request)
            );

            _schemaCache.AnalyzeMutations(schemaMutation);
            return response.Version;
        });
    }

    /// <summary>
    /// Deletes entire collection of entities along with its schema. After this operation there will be nothing left
    /// of the data that belong to the specified entity type.
    /// </summary>
    /// <param name="entityType">type of the entity which collection should be deleted</param>
    /// <returns>TRUE if collection was successfully deleted</returns>
    public bool DeleteCollection(string entityType)
    {
        return DeleteCollectionAsync(entityType).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="DeleteCollection"/>.
    /// </summary>
    public async Task<bool> DeleteCollectionAsync(string entityType, CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(
            async _ =>
            {
                var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                    evitaSessionService.DeleteCollectionAsync(
                        new GrpcDeleteCollectionRequest { EntityType = entityType },
                        cancellationToken: cancellationToken
                    ).ResponseAsync
                ).ConfigureAwait(false);
                _schemaCache.RemoveLatestEntitySchema(entityType);
                return grpcResponse.Deleted;
            }
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Method removes existing hierarchical entity in collection by its primary key. Method also removes all entities
    /// of the same type that are transitively referencing the removed entity as its parent. All entities of other entity
    /// types that reference removed entities in their <see cref="ISealedEntity.GetReference(string, int)"/> still keep
    /// the data untouched.
    /// </summary>
    /// <param name="entityType">type of entity to delete</param>
    /// <param name="primaryKey">primary key of entity to delete</param>
    /// <returns>number of removed entities</returns>
    public int DeleteEntityAndItsHierarchy(string entityType, int primaryKey)
    {
        return DeleteEntityAndItsHierarchyAsync(entityType, primaryKey).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="DeleteEntityAndItsHierarchy(string,int)"/>.
    /// </summary>
    public async Task<int> DeleteEntityAndItsHierarchyAsync(string entityType, int primaryKey,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(async _ =>
        {
            var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                evitaSessionService.DeleteEntityAndItsHierarchyAsync(
                    new GrpcDeleteEntityRequest { EntityType = entityType, PrimaryKey = primaryKey },
                    cancellationToken: cancellationToken
                ).ResponseAsync
            ).ConfigureAwait(false);
            return grpcResponse.DeletedEntities;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Method removes existing hierarchical entity in collection by its primary key. Method also removes all entities
    /// of the same type that are transitively referencing the removed entity as its parent. All entities of other entity
    /// types that reference removed entities in their <see cref="ISealedEntity.GetReference(string, int)"/> still keep
    /// the data untouched.
    /// </summary>
    /// <param name="entityType">type of entity to delete</param>
    /// <param name="primaryKey">primary key of entity to delete</param>
    /// <param name="require">additional requirements on the entity to delete</param>
    /// <returns>number of removed entities</returns>
    public DeletedHierarchy<ISealedEntity> DeleteEntityAndItsHierarchy(string entityType, int primaryKey,
        params IEntityContentRequire[] require)
    {
        return DeleteEntityHierarchyInternal(entityType, primaryKey, require);
    }

    /// <summary>
    /// Method removes existing entity in collection by its primary key. All entities of other entity types that reference
    /// removed entity in their <see cref="ISealedEntity.GetReference(string, int)"/> still keep the data untouched.
    /// </summary>
    /// <param name="entityType">type of the entity to be removed</param>
    /// <param name="primaryKey">primary key of the entity to be removed</param>
    /// <returns>true if entity existed and was removed</returns>
    public bool DeleteEntity(string entityType, int primaryKey)
    {
        return DeleteEntityAsync(entityType, primaryKey).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="DeleteEntity(string,int)"/>.
    /// </summary>
    public async Task<bool> DeleteEntityAsync(string entityType, int primaryKey,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(async _ =>
        {
            var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                evitaSessionService.DeleteEntityAsync(
                    new GrpcDeleteEntityRequest { EntityType = entityType, PrimaryKey = primaryKey },
                    cancellationToken: cancellationToken
                ).ResponseAsync
            ).ConfigureAwait(false);
            return grpcResponse.Entity is not null || grpcResponse.EntityReference is not null;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Method archives an existing entity in the collection by its primary key - the entity is moved to the
    /// archived scope (soft delete). All other entities of other types that reference the archived entity keep
    /// their data untouched.
    /// </summary>
    /// <param name="entityType">type of the entity to archive</param>
    /// <param name="primaryKey">primary key of the entity to archive</param>
    /// <returns>true if the entity existed and was archived</returns>
    public bool ArchiveEntity(string entityType, int primaryKey)
    {
        return ArchiveEntityAsync(entityType, primaryKey).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="ArchiveEntity(string,int)"/>.
    /// </summary>
    public async Task<bool> ArchiveEntityAsync(string entityType, int primaryKey,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(async _ =>
        {
            var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                evitaSessionService.ArchiveEntityAsync(
                    new GrpcArchiveEntityRequest { EntityType = entityType, PrimaryKey = primaryKey },
                    cancellationToken: cancellationToken
                ).ResponseAsync
            ).ConfigureAwait(false);
            return grpcResponse.Entity is not null || grpcResponse.EntityReference is not null;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Method archives an existing entity in the collection by its primary key and returns its body fetched
    /// according to the `require` definition.
    /// </summary>
    public ISealedEntity? ArchiveEntity(string entityType, int primaryKey, params IEntityContentRequire[] require)
    {
        return ArchiveEntityAsync(entityType, primaryKey, require).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="ArchiveEntity(string,int,IEntityContentRequire[])"/>.
    /// </summary>
    public async Task<ISealedEntity?> ArchiveEntityAsync(string entityType, int primaryKey,
        IEntityContentRequire[] require, CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(async _ =>
        {
            var stringWithParameters = ToStringWithParameterExtraction(require);
            var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                evitaSessionService.ArchiveEntityAsync(
                    new GrpcArchiveEntityRequest
                    {
                        EntityType = entityType,
                        PrimaryKey = primaryKey,
                        Require = stringWithParameters.Query,
                        PositionalQueryParams =
                        {
                            stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)
                        }
                    },
                    cancellationToken: cancellationToken
                ).ResponseAsync
            ).ConfigureAwait(false);
            return grpcResponse.Entity is not null
                ? EntityConverter.ToEntity<ISealedEntity>(
                    entity => _schemaCache.GetEntitySchemaOrThrow(
                        entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                    ),
                    grpcResponse.Entity,
                    new EvitaRequest(
                        IQueryConstraints.Query(
                            Collection(entityType),
                            Require(
                                EntityFetch(require)
                            )
                        ),
                        DateTimeOffset.Now
                    )
                )
                : null;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Method restores a previously archived entity in the collection by its primary key - the entity is moved
    /// back to the live scope.
    /// </summary>
    /// <param name="entityType">type of the entity to restore</param>
    /// <param name="primaryKey">primary key of the entity to restore</param>
    /// <returns>true if the entity existed and was restored</returns>
    public bool RestoreEntity(string entityType, int primaryKey)
    {
        return RestoreEntityAsync(entityType, primaryKey).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="RestoreEntity(string,int)"/>.
    /// </summary>
    public async Task<bool> RestoreEntityAsync(string entityType, int primaryKey,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(async _ =>
        {
            var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                evitaSessionService.RestoreEntityAsync(
                    new GrpcRestoreEntityRequest { EntityType = entityType, PrimaryKey = primaryKey },
                    cancellationToken: cancellationToken
                ).ResponseAsync
            ).ConfigureAwait(false);
            return grpcResponse.Entity is not null || grpcResponse.EntityReference is not null;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Method restores a previously archived entity in the collection by its primary key and returns its body
    /// fetched according to the `require` definition.
    /// </summary>
    public ISealedEntity? RestoreEntity(string entityType, int primaryKey, params IEntityContentRequire[] require)
    {
        return RestoreEntityAsync(entityType, primaryKey, require).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="RestoreEntity(string,int,IEntityContentRequire[])"/>.
    /// </summary>
    public async Task<ISealedEntity?> RestoreEntityAsync(string entityType, int primaryKey,
        IEntityContentRequire[] require, CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(async _ =>
        {
            var stringWithParameters = ToStringWithParameterExtraction(require);
            var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                evitaSessionService.RestoreEntityAsync(
                    new GrpcRestoreEntityRequest
                    {
                        EntityType = entityType,
                        PrimaryKey = primaryKey,
                        Require = stringWithParameters.Query,
                        PositionalQueryParams =
                        {
                            stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)
                        }
                    },
                    cancellationToken: cancellationToken
                ).ResponseAsync
            ).ConfigureAwait(false);
            return grpcResponse.Entity is not null
                ? EntityConverter.ToEntity<ISealedEntity>(
                    entity => _schemaCache.GetEntitySchemaOrThrow(
                        entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                    ),
                    grpcResponse.Entity,
                    new EvitaRequest(
                        IQueryConstraints.Query(
                            Collection(entityType),
                            Require(
                                EntityFetch(require)
                            )
                        ),
                        DateTimeOffset.Now
                    )
                )
                : null;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Method removes all entities that match passed query. All entities of other entity types that reference removed
    /// entities in their {@link SealedEntity#getReference(String, int)} still keep the data untouched. This variant of
    /// the delete by query method allows returning partial of full bodies of the removed entities.
    /// 
    /// Beware: you need to provide <see cref="Page"/> or <see cref="Strip"/> in the query to control the maximum number of removed
    /// entities. Otherwise, the default value of maximum of `20` entities to remove will be used.
    /// </summary>
    /// <param name="query">query to specify which entities should be deleted</param>
    /// <returns>bodies of deleted entities according to <see cref="Query.Require"/></returns>
    public ISealedEntity[] DeleteSealedEntitiesAndReturnBodies(Query query)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var evitaRequest = new EvitaRequest(
                query,
                DateTimeOffset.Now,
                typeof(ISealedEntity)
            );
            var stringWithParameters = query.ToStringWithParametersExtraction();
            var grpcResponse = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntities(
                    new GrpcDeleteEntitiesRequest
                    {
                        Query = stringWithParameters.Query,
                        PositionalQueryParams =
                        {
                            stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)
                        }
                    }
                )
            );
            return grpcResponse.DeletedEntityBodies
                .Select(
                    it => EntityConverter.ToEntity<ISealedEntity>(
                        entity => _schemaCache.GetEntitySchemaOrThrow(
                            entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                        ),
                        it,
                        evitaRequest
                    )
                )
                .ToArray();
        });
    }

    /// <summary>
    /// Method removes existing entity in collection by its primary key. All entities of other entity types that reference
    /// removed entity in their <see cref="ISealedEntity.GetReference(string, int)"/> still keep the data untouched.
    /// </summary>
    /// <param name="entityType">type of the entity that should be deleted</param>
    /// <param name="primaryKey">primary key of the entity that should be deleted</param>
    /// <param name="require">specifications to fetch entity to be deleted and returned from the method</param>
    /// <returns>removed entity fetched according to `require` definition</returns>
    public ISealedEntity? DeleteEntity(string entityType, int primaryKey, params IEntityContentRequire[] require)
    {
        return DeleteEntityInternal(entityType, primaryKey, require);
    }

    /// <summary>
    /// Method removes all entities that match passed query. All entities of other entity types that reference removed
    /// entities in their <see cref="ISealedEntity.GetReference(string, int)"/> still keep the data untouched.
    /// 
    /// Beware: you need to provide <see cref="Page"/> or <see cref="Strip"/> in the query to control the maximum number of removed
    /// entities. Otherwise, the default value of maximum of `20` entities to remove will be used.
    /// </summary>
    /// <param name="query">query to specify which entities should be deleted</param>
    /// <returns>number of deleted entities</returns>
    public int DeleteEntities(Query query)
    {
        return DeleteEntitiesAsync(query).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="DeleteEntities"/>.
    /// </summary>
    public async Task<int> DeleteEntitiesAsync(Query query, CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(async _ =>
        {
            var stringWithParameters = ToStringWithParameterExtraction(query);
            var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                evitaSessionService.DeleteEntitiesAsync(
                    new GrpcDeleteEntitiesRequest
                    {
                        Query = stringWithParameters.Query,
                        PositionalQueryParams =
                        {
                            stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)
                        }
                    },
                    cancellationToken: cancellationToken
                ).ResponseAsync
            ).ConfigureAwait(false);
            return grpcResponse.DeletedEntities;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Renames entire collection of entities along with its schema. After this operation there will be nothing left
    /// of the data that belong to the specified entity type, and entity collection under the new name becomes available.
    /// If you need to rename entity collection to a name of existing collection use
    /// the <see cref="ReplaceCollection(string, string)"/> method instead.
    /// 
    /// In case exception occurs the original collection (`entityType`) is guaranteed to be untouched,
    /// and the `newName` will not be present.
    /// </summary>
    /// <param name="entityType">current name of the entity collection</param>
    /// <param name="newName">new name of the entity collection</param>
    /// <returns>TRUE if collection was successfully renamed</returns>
    public bool RenameCollection(string entityType, string newName)
    {
        return RenameCollectionAsync(entityType, newName).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="RenameCollection"/>.
    /// </summary>
    public async Task<bool> RenameCollectionAsync(string entityType, string newName,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(
            async _ =>
            {
                var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                    evitaSessionService.RenameCollectionAsync(
                        new GrpcRenameCollectionRequest { EntityType = entityType, NewName = newName },
                        cancellationToken: cancellationToken
                    ).ResponseAsync
                ).ConfigureAwait(false);
                _schemaCache.RemoveLatestEntitySchema(entityType);
                return grpcResponse.Renamed;
            }
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces existing entity collection of particular with the contents of the another collection. When this method
    /// is successfully finished, the entity collection `entityTypeToBeReplaced` will be known under the name of the
    /// `entityTypeToBeReplacedWith` and the original contents of the `entityTypeToBeReplaced` will be purged entirely.
    /// 
    /// In case exception occurs, both the original collection (`entityTypeToBeReplaced`) and replaced collection
    /// (`entityTypeToBeReplacedWith`) are guaranteed to be untouched.
    /// </summary>
    /// <param name="entityTypeToBeReplaced">name of the collection that will be replaced and dropped</param>
    /// <param name="entityTypeToBeReplacedWith">name of the collection that will become the successor of the original catalog</param>
    /// <returns>TRUE if collection was successfully replaced</returns>
    public bool ReplaceCollection(string entityTypeToBeReplaced, string entityTypeToBeReplacedWith)
    {
        return ReplaceCollectionAsync(entityTypeToBeReplaced, entityTypeToBeReplacedWith).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="ReplaceCollection"/>.
    /// </summary>
    public async Task<bool> ReplaceCollectionAsync(string entityTypeToBeReplaced,
        string entityTypeToBeReplacedWith, CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(
            async _ =>
            {
                var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                    evitaSessionService.ReplaceCollectionAsync(
                        new GrpcReplaceCollectionRequest
                        {
                            EntityTypeToBeReplaced = entityTypeToBeReplaced,
                            EntityTypeToBeReplacedWith = entityTypeToBeReplacedWith
                        },
                        cancellationToken: cancellationToken
                    ).ResponseAsync
                ).ConfigureAwait(false);
                _schemaCache.RemoveLatestEntitySchema(entityTypeToBeReplaced);
                _schemaCache.RemoveLatestEntitySchema(entityTypeToBeReplacedWith);
                return grpcResponse.Replaced;
            }
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Method alters one of the <see cref="IEntitySchema"/> of the catalog this session is tied to.
    /// All mutations will be applied or none of them (method call is atomic). It's also idempotent - it means that
    /// when the method is called multiple times with same mutations the changes occur only once.
    /// </summary>
    /// <param name="schemaMutation">the builder that contains the mutations in the entity schema</param>
    /// <returns>version of the altered schema or current version if no modification occurred.</returns>
    public int UpdateEntitySchema(ModifyEntitySchemaMutation schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var grpcSchemaMutation =
                ModifyEntitySchemaMutationConverter.Convert(schemaMutation);
            var request = new GrpcUpdateEntitySchemaRequest { SchemaMutation = grpcSchemaMutation };
            var response = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateEntitySchema(request)
            );
            _schemaCache.AnalyzeMutations(schemaMutation);
            return response.Version;
        });
    }

    /// <summary>
    /// Method alters one of the <see cref="IEntitySchema"/> of the catalog this session is tied to.
    /// The method is equivalent to <see cref="UpdateEntitySchema(ModifyEntitySchemaMutation)"/> but accepts the original builder.
    /// This method variant is present as a shortcut option for the developers.
    /// </summary>
    /// <param name="entitySchemaBuilder">the builder that contains the mutations in the entity schema</param>
    /// <returns>version of the altered schema or current version if no modification occurred.</returns>
    public int UpdateEntitySchema(IEntitySchemaBuilder entitySchemaBuilder)
    {
        var mutation = entitySchemaBuilder.ToMutation();
        return mutation is not null
            ? UpdateEntitySchema(mutation)
            : GetEntitySchemaOrThrow(entitySchemaBuilder.Name).Version;
    }

    /// <summary>
    /// Method alters one of the <see cref="IEntitySchema"/> of the catalog this session is tied to. All
    /// mutations will be applied or none of them (method call is atomic). The method call is idempotent - it means that
    /// when the method is called multiple times with same mutations the changes occur only once.
    /// </summary>
    /// <param name="schemaMutation">the builder that contains the mutations in the entity schema</param>
    /// <returns>possibly updated body of the <see cref="IEntitySchema"/> or the original schema if no change occurred</returns>
    public ISealedEntitySchema UpdateAndFetchEntitySchema(ModifyEntitySchemaMutation schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var grpcSchemaMutation =
                ModifyEntitySchemaMutationConverter.Convert(schemaMutation);
            var request = new GrpcUpdateEntitySchemaRequest { SchemaMutation = grpcSchemaMutation };

            var response = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateAndFetchEntitySchema(request)
            );

            var updatedSchema = EntitySchemaConverter.Convert(response.EntitySchema);
            _schemaCache.AnalyzeMutations(schemaMutation);
            _schemaCache.SetLatestEntitySchema(updatedSchema);
            return new EntitySchemaDecorator(GetCatalogSchema, updatedSchema);
        });
    }

    /// <summary>
    /// Switches catalog to the <see cref="Session.CatalogState.Alive"/> state and terminates the Evita session so that next session is
    /// operating in the new catalog state.
    /// 
    /// Session is <see cref="Close()"/> only when the state transition successfully occurs and this is signalized
    /// by return value.
    /// </summary>
    /// <returns></returns>
    /// <summary>
    /// Creates a backup of the catalog this session is bound to and returns the status of the server task that
    /// generates the backup file. Use <see cref="EvitaClientManagement.WaitForTaskCompletionAsync"/> to wait for
    /// the file to become available and <see cref="EvitaClientManagement.FetchFileAsync"/> to download it.
    /// </summary>
    /// <param name="pastMoment">optional moment in the past the backup should reflect (requires WAL history)</param>
    /// <param name="catalogVersion">optional exact catalog version the backup should reflect</param>
    /// <param name="includingWal">when true the backup includes the write-ahead log</param>
    public TaskStatus BackupCatalog(DateTimeOffset? pastMoment = null, long? catalogVersion = null,
        bool includingWal = false)
    {
        return BackupCatalogAsync(pastMoment, catalogVersion, includingWal).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="BackupCatalog"/>.
    /// </summary>
    public async Task<TaskStatus> BackupCatalogAsync(DateTimeOffset? pastMoment = null, long? catalogVersion = null,
        bool includingWal = false, CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcBackupCatalogRequest request = new GrpcBackupCatalogRequest { IncludingWAL = includingWal };
        if (pastMoment.HasValue)
        {
            request.PastMoment = EvitaDataTypesConverter.ToGrpcDateTime(pastMoment.Value);
        }
        if (catalogVersion.HasValue)
        {
            request.CatalogVersion = catalogVersion.Value;
        }
        GrpcBackupCatalogResponse grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(
            evitaSessionService => evitaSessionService.BackupCatalogAsync(request, cancellationToken: cancellationToken)
                .ResponseAsync
        ).ConfigureAwait(false);
        return ManagementConverter.ToTaskStatus(grpcResponse.TaskStatus);
    }

    /// <summary>
    /// Creates a full backup (including indexes and WAL) of the catalog this session is bound to and returns
    /// the status of the server task that generates the backup file.
    /// </summary>
    public TaskStatus FullBackupCatalog()
    {
        return FullBackupCatalogAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="FullBackupCatalog"/>.
    /// </summary>
    public async Task<TaskStatus> FullBackupCatalogAsync(CancellationToken cancellationToken = default)
    {
        AssertActive();
        GrpcFullBackupCatalogResponse grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(
            evitaSessionService => evitaSessionService.FullBackupCatalogAsync(new Empty(),
                cancellationToken: cancellationToken).ResponseAsync
        ).ConfigureAwait(false);
        return ManagementConverter.ToTaskStatus(grpcResponse.TaskStatus);
    }

    /// <summary>
    /// Switches the catalog from the warming-up state to the alive (transactional) state and closes the session,
    /// reporting the go-live progress through the optional observer. Returns the versions the catalog reached.
    /// </summary>
    /// <param name="progressObserver">optional observer receiving the go-live progress in percent (0-100)</param>
    /// <param name="cancellationToken">token cancelling the wait for the go-live completion</param>
    public async Task<CommitVersions> GoLiveAndCloseWithProgressAsync(IProgress<int>? progressObserver = null,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        ChannelInvoker channel = new SharedChannelSupplier(_cdcChannel!).GetChannel();
        SessionIdHolder.SetSessionId(SessionId.ToString());
        var service = new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker);
        using var call = service.GoLiveAndCloseWithProgress(new Empty(), cancellationToken: cancellationToken);
        CommitVersions versions = new CommitVersions(0, 0);
        while (await MoveNextTranslated(call.ResponseStream, cancellationToken).ConfigureAwait(false))
        {
            GrpcGoLiveAndCloseWithProgressResponse message = call.ResponseStream.Current;
            progressObserver?.Report(message.ProgressInPercent);
            if (message.ProgressInPercent >= 100)
            {
                versions = new CommitVersions(message.CatalogVersion, message.CatalogSchemaVersion);
            }
        }
        CloseInternally();
        return versions;
    }

    /// <summary>
    /// Closes the session and tracks the commit of its transaction through the individual server-side phases.
    /// The session becomes unusable immediately; the returned <see cref="CommitProgress"/> exposes a task per
    /// commit phase. When `rollback` is true the transaction is discarded instead of committed.
    /// </summary>
    public CommitProgress CloseNowWithProgress(bool rollback = false)
    {
        AssertActive();
        CommitProgress progress = new CommitProgress();
        ChannelInvoker channel = new SharedChannelSupplier(_cdcChannel!).GetChannel();
        SessionIdHolder.SetSessionId(SessionId.ToString());
        var service = new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker);
        var call = service.CloseWithProgress(new GrpcCloseWithProgressRequest
        {
            CatalogName = CatalogName,
            Rollback = rollback
        });
        // the session is locally unusable from this point on - the commit continues on the server
        CloseInternally();
        _ = Task.Run(async () =>
        {
            try
            {
                CommitVersions lastVersions = new CommitVersions(0, 0);
                while (await call.ResponseStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
                {
                    GrpcCloseWithProgressResponse message = call.ResponseStream.Current;
                    lastVersions = new CommitVersions(message.CatalogVersion, message.CatalogSchemaVersion);
                    progress.CompletePhase(message.FinishedPhase, lastVersions);
                }
                // the stream may end without reporting every phase (e.g. read-only session or nothing to commit)
                progress.Complete(lastVersions);
            }
            catch (RpcException rpcException)
            {
                progress.Fail(TranslateRpcException(rpcException));
            }
            catch (Exception e)
            {
                progress.Fail(e);
            }
            finally
            {
                call.Dispose();
            }
        });
        return progress;
    }

    /// <summary>
    /// Closes the session and blocks until the commit of its transaction reaches the passed phase. Returns
    /// the versions the catalog reached in that phase.
    /// </summary>
    public CommitVersions CloseWhen(EvitaClientTransaction.CommitBehavior commitBehavior)
    {
        return CloseNowWithProgress().On(commitBehavior).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="CloseWhen"/>.
    /// </summary>
    public Task<CommitVersions> CloseWhenAsync(EvitaClientTransaction.CommitBehavior commitBehavior)
    {
        return CloseNowWithProgress().On(commitBehavior);
    }

    public bool GoLiveAndClose()
    {
        return GoLiveAndCloseAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GoLiveAndClose"/>.
    /// </summary>
    public async Task<bool> GoLiveAndCloseAsync(CancellationToken cancellationToken = default)
    {
        AssertActive();
        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
            evitaSessionService.GoLiveAndCloseAsync(new Empty(), cancellationToken: cancellationToken).ResponseAsync
        ).ConfigureAwait(false);
        var success = grpcResponse.Success;
        if (success)
        {
            CloseInternally();
        }

        return success;
    }

    /// <summary>
    /// Terminates Evita session and releases all used resources. This method renders the session unusable and any further
    /// calls to this session should end up with <see cref="InstanceTerminatedException"/>
    /// 
    /// This method is idempotent and may be called multiple times. Only first call is really processed and others are
    /// ignored.
    /// </summary>
    public void Close()
    {
        CloseAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="Close"/>.
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Active)
        {
            await ExecuteWithBlockingEvitaSessionServiceAsync(session =>
                session.CloseAsync(new GrpcCloseRequest
                {
                    CommitBehaviour = EvitaEnumConverter.ToGrpcCommitBehavior(CommitBehavior),
                    CatalogName = CatalogName
                }, cancellationToken: cancellationToken).ResponseAsync
            ).ConfigureAwait(false);
            CloseInternally();
        }
    }

    /// <summary>
    /// Method internally closes the session
    /// </summary>
    private void CloseInternally()
    {
        if (!Active) return;
        Active = false;
        _onTerminationCallback.Invoke(this);
    }

    /// <summary>
    /// Method alters the <see cref="ICatalogSchema"/> of the catalog this session is tied to. All mutations will be
    /// applied or none of them (method call is atomic). The method call is idempotent - it means that when the method
    /// is called multiple times with same mutations the changes occur only once.
    /// </summary>
    /// <param name="schemaMutation">array of mutations that needs to be applied on current version of <see cref="ICatalogSchema"/></param>
    /// <returns>possibly updated body of the <see cref="ICatalogSchema"/> or the original schema if no change occurred</returns>
    public ISealedCatalogSchema UpdateAndFetchCatalogSchema(params ILocalCatalogSchemaMutation[] schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var grpcSchemaMutations = schemaMutation
                .Select(CatalogSchemaMutationConverter.Convert)
                .ToList();

            var request = new GrpcUpdateCatalogSchemaRequest { SchemaMutations = { grpcSchemaMutations } };

            var response = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateAndFetchCatalogSchema(request)
            );

            var updatedCatalogSchema =
                CatalogSchemaConverter.Convert(response.CatalogSchema, EntitySchemaAccessor);
            ISealedCatalogSchema updatedSchema =
                new CatalogSchemaDecorator(updatedCatalogSchema, GetEntitySchemaOrThrow);
            _schemaCache.AnalyzeMutations(schemaMutation);
            _schemaCache.SetLatestCatalogSchema(updatedCatalogSchema);
            return updatedSchema;
        });
    }

    /// <summary>
    /// Method alters the <see cref="ICatalogSchema"/> of the catalog this session is tied to. The method is equivalent
    /// to <see cref="UpdateAndFetchCatalogSchema(ILocalCatalogSchemaMutation[])"/> but accepts the original builder. This method
    /// variant is present as a shortcut option for the developers.
    /// </summary>
    /// <param name="catalogSchemaBuilder">the builder that contains the mutations in the catalog schema</param>
    /// <returns>possibly updated body of the <see cref="ICatalogSchema"/> or the original schema if no change occurred</returns>
    public ISealedCatalogSchema UpdateAndFetchCatalogSchema(ICatalogSchemaBuilder catalogSchemaBuilder)
    {
        Assert.IsTrue(
            catalogSchemaBuilder.Name.Equals(CatalogName),
            "Schema builder targets `" + catalogSchemaBuilder.Name + "` catalog, but the session targets `" +
            CatalogName + "` catalog!"
        );
        var modifyCatalogSchemaMutation = catalogSchemaBuilder.ToMutation();
        return modifyCatalogSchemaMutation is not null
            ? UpdateAndFetchCatalogSchema(modifyCatalogSchemaMutation.SchemaMutations)
            : GetCatalogSchema();
    }

    /// <summary>
    /// Method alters the {@link CatalogSchemaContract} of the catalog this session is tied to. The method is equivalent
    /// to <see cref="UpdateCatalogSchema(ILocalCatalogSchemaMutation[])"/> but accepts the original builder. This method variant
    /// is present as a shortcut option for the developers.
    /// </summary>
    /// <param name="catalogSchemaBuilder">the builder that contains the mutations in the catalog schema</param>
    /// <returns>version of the altered schema or current version if no modification occurred.</returns>
    public int UpdateCatalogSchema(ICatalogSchemaBuilder catalogSchemaBuilder)
    {
        Assert.IsTrue(
            catalogSchemaBuilder.Name.Equals(CatalogName),
            "Schema builder targets `" + catalogSchemaBuilder.Name + "` catalog, but the session targets `" +
            CatalogName + "` catalog!"
        );
        var modifyCatalogSchemaMutation = catalogSchemaBuilder.ToMutation();
        return modifyCatalogSchemaMutation is not null
            ? UpdateCatalogSchema(modifyCatalogSchemaMutation.SchemaMutations)
            : GetCatalogSchema().Version;
    }

    /// <summary>
    /// Extracts extra results from gRPC response.
    /// </summary>
    /// <param name="grpcResponse">grpc response received from the server</param>
    /// <param name="evitaRequest">instance of EvitaRequest required for correct deserialization</param>
    /// <returns></returns>
    private IEvitaResponseExtraResult[] GetEvitaResponseExtraResults(GrpcQueryResponse grpcResponse,
        EvitaRequest evitaRequest)
    {
        return grpcResponse.ExtraResults is not null
            ? ResponseConverter.ToExtraResults(
                sealedEntity => _schemaCache.GetEntitySchemaOrThrow(
                    sealedEntity.EntityType, sealedEntity.SchemaVersion,
                    FetchEntitySchema, GetCatalogSchema
                ),
                evitaRequest,
                grpcResponse.ExtraResults
            )
            : [];
    }

    /// <summary>
    /// Returns catalog schema of the catalog this session is connected to.
    /// </summary>
    public ISealedCatalogSchema GetCatalogSchema()
    {
        AssertActive();
        return _schemaCache.GetLatestCatalogSchema(FetchCatalogSchema, GetEntitySchema);
    }

    /// <summary>
    /// Returns catalog schema of the catalog this session is connected to.
    /// </summary>
    public ISealedCatalogSchema GetCatalogSchema(EvitaClient evita)
    {
        AssertActive();
        return _schemaCache.GetLatestCatalogSchema(
            () => Active
                ? FetchCatalogSchema()
                : evita.QueryCatalog(
                    CatalogName,
                    session => session.FetchCatalogSchema()),
            entityType => Active
                ? GetEntitySchema(entityType)
                : evita.QueryCatalog(
                    CatalogName,
                    session => session.GetEntitySchema(entityType))
        );
    }

    /// <summary>
    /// This internal method will physically call over the network and fetch actual <see cref="CatalogSchema"/>.
    /// </summary>
    private CatalogSchema FetchCatalogSchema()
    {
        var grpcResponse = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetCatalogSchema(new GrpcGetCatalogSchemaRequest())
        );
        return CatalogSchemaConverter.Convert(
            grpcResponse.CatalogSchema, EntitySchemaAccessor
        );
    }

    /// <summary>
    /// Async variant of <see cref="GetCatalogSchema()"/>, which additionally <b>primes the schema cache</b> so
    /// that the lazily invoked catalog-schema supplier never reaches the blocking <see cref="FetchCatalogSchema"/>.
    /// Hosts that cannot block should call it once, before the first query - see <see cref="GetEntitySchemaAsync"/>.
    /// </summary>
    public async Task<ISealedCatalogSchema> GetCatalogSchemaAsync(CancellationToken cancellationToken = default)
    {
        AssertActive();
        CatalogSchema catalogSchema = await FetchCatalogSchemaAsync(cancellationToken).ConfigureAwait(false);
        _schemaCache.SetCatalogSchema(catalogSchema);
        return new CatalogSchemaDecorator(catalogSchema, GetEntitySchema);
    }

    /// <summary>
    /// Async counterpart of <see cref="FetchCatalogSchema"/>.
    /// </summary>
    private async Task<CatalogSchema> FetchCatalogSchemaAsync(CancellationToken cancellationToken = default)
    {
        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
            evitaSessionService
                .GetCatalogSchemaAsync(new GrpcGetCatalogSchemaRequest(), cancellationToken: cancellationToken)
                .ResponseAsync
        ).ConfigureAwait(false);
        return CatalogSchemaConverter.Convert(
            grpcResponse.CatalogSchema, EntitySchemaAccessor
        );
    }

    /// <summary>
    /// Returns schema definition for entity of specified type or throws a standardized exception.
    /// </summary>
    public ISealedEntitySchema GetEntitySchemaOrThrow(string entityType)
    {
        AssertActive();
        return GetEntitySchema(entityType) ?? throw new CollectionNotFoundException(entityType);
    }

    /// <summary>
    /// Returns schema definition for entity of specified type.
    /// </summary>
    public ISealedEntitySchema? GetEntitySchema(string entityType)
    {
        AssertActive();
        return _schemaCache.GetLatestEntitySchema(entityType, FetchEntitySchema, GetCatalogSchema);
    }

    /// <summary>
    /// Async variant of <see cref="GetEntitySchema"/>, which additionally <b>primes the schema cache</b> so that
    /// later query-response conversions find the schema instead of reaching for the blocking accessor.
    ///
    /// Hosts that cannot block - Blazor WebAssembly - must call this for every entity type they will touch
    /// (including the types of referenced entities) before issuing the first query; see
    /// <c>documentation/architecture.md</c>. Unlike <see cref="GetEntitySchema"/> this method always performs
    /// a network call, because it is meant for priming rather than for repeated lookups.
    /// </summary>
    public async Task<ISealedEntitySchema?> GetEntitySchemaAsync(string entityType,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        EntitySchema? entitySchema = await FetchEntitySchemaAsync(entityType, cancellationToken)
            .ConfigureAwait(false);
        if (entitySchema is null)
        {
            return null;
        }
        _schemaCache.SetEntitySchema(entitySchema);
        return new EntitySchemaDecorator(GetCatalogSchema, entitySchema);
    }

    /// <summary>
    /// Async variant of <see cref="GetEntitySchemaOrThrow"/> - see <see cref="GetEntitySchemaAsync"/>.
    /// </summary>
    public async Task<ISealedEntitySchema> GetEntitySchemaOrThrowAsync(string entityType,
        CancellationToken cancellationToken = default)
    {
        return await GetEntitySchemaAsync(entityType, cancellationToken).ConfigureAwait(false)
               ?? throw new CollectionNotFoundException(entityType);
    }

    /// <summary>
    /// Async counterpart of <see cref="FetchEntitySchema"/> - physically fetches the schema over the network
    /// without blocking the calling thread.
    /// </summary>
    private async Task<EntitySchema?> FetchEntitySchemaAsync(string entityType,
        CancellationToken cancellationToken = default)
    {
        var grpcResponse = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
            evitaSessionService
                .GetEntitySchemaAsync(new GrpcEntitySchemaRequest { EntityType = entityType },
                    cancellationToken: cancellationToken)
                .ResponseAsync
        ).ConfigureAwait(false);
        if (grpcResponse.EntitySchema is null)
        {
            return null;
        }

        return EntitySchemaConverter.Convert(grpcResponse.EntitySchema);
    }

    /// <summary>
    /// Method inserts to or updates entity according to passed entity builder. Direct link to <see cref="UpsertEntity(EvitaDB.Client.Models.Data.Mutations.IEntityMutation)"/>
    /// </summary>
    /// <param name="entityBuilder">builder for applying more mutations to the entity</param>
    public EntityReference UpsertEntity(IEntityBuilder entityBuilder)
    {
        var mutation = entityBuilder.ToMutation();
        return mutation is not null
            ? UpsertEntity(mutation)
            : new EntityReference(entityBuilder.Type, entityBuilder.PrimaryKey);
    }

    /// <summary>
    /// Async variant of <see cref="UpsertEntity(IEntityBuilder)"/>.
    /// </summary>
    public async Task<EntityReference> UpsertEntityAsync(IEntityBuilder entityBuilder,
        CancellationToken cancellationToken = default)
    {
        var mutation = entityBuilder.ToMutation();
        return mutation is not null
            ? await UpsertEntityAsync(mutation, cancellationToken).ConfigureAwait(false)
            : new EntityReference(entityBuilder.Type, entityBuilder.PrimaryKey);
    }

    /// <summary>
    /// Method inserts to or updates entity in collection according to passed set of mutations.
    /// </summary>
    /// <param name="entityMutation">list of mutation snippets that alter or form the entity</param>
    public EntityReference UpsertEntity(IEntityMutation entityMutation)
    {
        return UpsertEntityAsync(entityMutation).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="UpsertEntity(IEntityMutation)"/>.
    /// </summary>
    public async Task<EntityReference> UpsertEntityAsync(IEntityMutation entityMutation,
        CancellationToken cancellationToken = default)
    {
        AssertActive();
        return await ExecuteInTransactionIfPossibleAsync(async _ =>
        {
            var grpcEntityMutation = EntityMutationConverter.Convert(entityMutation);
            var grpcResult = await ExecuteWithBlockingEvitaSessionServiceAsync(evitaSessionService =>
                evitaSessionService.UpsertEntityAsync(
                    new GrpcUpsertEntityRequest { EntityMutation = grpcEntityMutation },
                    cancellationToken: cancellationToken
                ).ResponseAsync
            ).ConfigureAwait(false);
            // the server returns `entityReferenceWithAssignedPrimaryKeys` when the upsert reassigned internal
            // reference primary keys (e.g. reflected references); the plain reference otherwise. The reassignment
            // map does not need client-side propagation: this client never assigns temporary internal primary
            // keys itself (the builder model does not create duplicate reference occurrences), so there is no
            // locally held key that could become stale - fetched entities always carry the server-assigned keys
            return grpcResult.ResponseCase switch
            {
                GrpcUpsertEntityResponse.ResponseOneofCase.EntityReference => new EntityReference(
                    grpcResult.EntityReference.EntityType, grpcResult.EntityReference.PrimaryKey
                ),
                GrpcUpsertEntityResponse.ResponseOneofCase.EntityReferenceWithAssignedPrimaryKeys =>
                    new EntityReference(
                        grpcResult.EntityReferenceWithAssignedPrimaryKeys.EntityType,
                        grpcResult.EntityReferenceWithAssignedPrimaryKeys.PrimaryKey
                    ),
                _ => throw new EvitaInternalError(
                    $"Unexpected upsert entity response case: {grpcResult.ResponseCase}"
                )
            };
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Shorthand method for <see cref="UpsertEntity(IEntityMutation)"/> that accepts <see cref="IEntityBuilder"/> that can produce
    /// mutation.
    /// </summary>
    /// <param name="entityBuilder">that contains changed entity state</param>
    /// <param name="require">require constraints to specify richness of returned entity</param>
    /// <returns>modified entity fetched according to `require` definition</returns>
    public ISealedEntity UpsertAndFetchEntity(IEntityBuilder entityBuilder, params IEntityContentRequire[] require)
    {
        var mutation = entityBuilder.ToMutation();
        return mutation is not null
            ? UpsertAndFetchEntity(mutation, require)
            : GetEntityOrThrow(entityBuilder.Type, entityBuilder.PrimaryKey!.Value, require);
    }

    /// <summary>
    /// Method inserts to or updates entity in collection according to passed set of mutations.
    /// </summary>
    /// <param name="entityMutation">list of mutation snippets that alter or form the entity</param>
    /// <param name="require">require constraints to specify richness of returned entity</param>
    /// <returns>modified entity fetched according to `require` definition</returns>
    public ISealedEntity UpsertAndFetchEntity(IEntityMutation entityMutation, params IEntityContentRequire[] require)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var grpcEntityMutation = EntityMutationConverter.Convert(entityMutation);
            var stringWithParameters = ToStringWithParameterExtraction(require);
            var grpcResponse = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpsertEntity(
                    new GrpcUpsertEntityRequest
                    {
                        EntityMutation = grpcEntityMutation,
                        Require = stringWithParameters.Query,
                        PositionalQueryParams =
                        {
                            stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)
                        }
                    }
                )
            );
            return EntityConverter.ToEntity<ISealedEntity>(
                entity => _schemaCache.GetEntitySchemaOrThrow(
                    entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                ),
                grpcResponse.Entity,
                new EvitaRequest(
                    IQueryConstraints.Query(
                        Collection(entityMutation.EntityType),
                        Require(
                            EntityFetch(require)
                        )
                    ),
                    DateTimeOffset.Now
                )
            );
        });
    }

    /// <summary>
    /// Return entity specified by passed constraints or throws exception when no entity is found.
    /// </summary>
    public ISealedEntity GetEntityOrThrow(string type, int primaryKey, params IEntityContentRequire[] require)
    {
        var entity = GetEntity(type, primaryKey, require);
        return entity ??
               throw new EvitaInvalidUsageException("Entity `" + type + "` with id `" + primaryKey +
                                                    "` doesn't exist!");
    }

    /// <summary>
    /// Creates entity builder for new entity without specified primary key needed to be inserted to the collection.
    /// </summary>
    /// <param name="entityType">type of the entity that should be created</param>
    /// <returns>builder instance to be filled up and stored via <see cref="UpsertEntity(EvitaDB.Client.Models.Data.IEntityBuilder)"/></returns>
    public IEntityBuilder CreateNewEntity(string entityType)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                IEntitySchema entitySchema;
                if (GetCatalogSchema().CatalogEvolutionModes.Contains(CatalogEvolutionMode.AddingEntityTypes))
                {
                    var schema = GetEntitySchema(entityType);
                    entitySchema = schema is not null ? schema : EntitySchema.InternalBuild(entityType);
                }
                else
                {
                    entitySchema = GetEntitySchemaOrThrow(entityType);
                }

                return new InitialEntityBuilder(entitySchema, null);
            }
        );
    }

    /// <summary>
    /// Creates entity builder for new entity with externally defined primary key needed to be inserted to
    /// the collection.
    /// </summary>
    /// <param name="entityType">type of the entity that should be created</param>
    /// <param name="primaryKey">externally assigned primary key for the entity</param>
    /// <returns>builder instance to be filled up and stored via <see cref="UpsertEntity(EvitaDB.Client.Models.Data.IEntityBuilder)"/></returns>
    public IEntityBuilder CreateNewEntity(string entityType, int primaryKey)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                IEntitySchema entitySchema;
                if (GetCatalogSchema().CatalogEvolutionModes.Contains(CatalogEvolutionMode.AddingEntityTypes))
                {
                    var schema = GetEntitySchema(entityType);
                    entitySchema = schema is not null ? schema : EntitySchema.InternalBuild(entityType);
                }
                else
                {
                    entitySchema = GetEntitySchemaOrThrow(entityType);
                }

                return new InitialEntityBuilder(entitySchema, primaryKey);
            }
        );
    }

    /// <summary>
    /// Streams the write-ahead-log history of the catalog this session is bound to as a lazily evaluated sequence.
    /// If the enumeration is abandoned prematurely the server stream is cancelled and the server is notified about it.
    /// </summary>
    /// <param name="request">request that specifies the criteria for the changes to be returned</param>
    /// <returns>lazily streamed changes to the specified scope of entities / schemas</returns>
    public IEnumerable<ChangeCatalogCapture> GetMutationsHistory(ChangeCatalogCaptureRequest request)
    {
        using CancellationTokenSource cancellation = new();
        IAsyncEnumerator<ChangeCatalogCapture> enumerator =
            GetMutationsHistoryAsync(request, cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        try
        {
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            cancellation.Cancel();
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Registers a change-data-capture subscription on the catalog this session is bound to and returns the
    /// captured changes as an async stream. The stream stays open until cancelled or the session is closed -
    /// server heartbeats keep it alive. The first message from the server (the subscription acknowledgement)
    /// is consumed internally.
    /// </summary>
    public async IAsyncEnumerable<ChangeCatalogCapture> RegisterChangeCatalogCaptureAsync(
        ChangeCatalogCaptureRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AssertActive();
        ChannelInvoker channel = new SharedChannelSupplier(_cdcChannel!).GetChannel();
        SessionIdHolder.SetSessionId(SessionId.ToString());
        var service = new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker);
        using var call = service.RegisterChangeCatalogCapture(
            ChangeCaptureConverter.ToGrpcRegisterChangeCatalogCaptureRequest(request),
            cancellationToken: cancellationToken
        );

        bool acknowledged = false;
        long lastHeartbeatIndex = -1;
        while (await MoveNextTranslated(call.ResponseStream, cancellationToken).ConfigureAwait(false))
        {
            GrpcRegisterChangeCatalogCaptureResponse message = call.ResponseStream.Current;
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
                lastHeartbeatIndex = message.HeartBeat?.Index ?? -1;
                continue;
            }

            switch (message.ResponseType)
            {
                case GrpcCaptureResponseType.Change:
                    yield return ChangeCaptureConverter.ToChangeCatalogCapture(message.Capture);
                    break;
                case GrpcCaptureResponseType.Heartbeat:
                    if (message.HeartBeat is not null)
                    {
                        if (lastHeartbeatIndex >= 0 && message.HeartBeat.Index != lastHeartbeatIndex + 1)
                        {
                            Console.WriteLine(
                                $"Change capture heartbeat discontinuity detected: expected index " +
                                $"{lastHeartbeatIndex + 1} but received {message.HeartBeat.Index}."
                            );
                        }
                        lastHeartbeatIndex = message.HeartBeat.Index;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Async variant of <see cref="GetMutationsHistory"/> - streams the write-ahead-log history of the catalog
    /// this session is bound to as an async sequence.
    /// </summary>
    public async IAsyncEnumerable<ChangeCatalogCapture> GetMutationsHistoryAsync(
        ChangeCatalogCaptureRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AssertActive();
        ChannelInvoker channel = new SharedChannelSupplier(_cdcChannel!).GetChannel();
        SessionIdHolder.SetSessionId(SessionId.ToString());
        var service = new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker);
        using var call = service.GetMutationsHistory(
            ChangeCaptureConverter.ToGrpcChangeCaptureRequest(request),
            cancellationToken: cancellationToken
        );
        while (await MoveNextTranslated(call.ResponseStream, cancellationToken).ConfigureAwait(false))
        {
            foreach (GrpcChangeCatalogCapture capture in call.ResponseStream.Current.ChangeCapture)
            {
                yield return ChangeCaptureConverter.ToChangeCatalogCapture(capture);
            }
        }
    }

    /// <summary>
    /// Moves the stream reader forward translating gRPC failures to evitaDB exceptions (async iterators cannot
    /// wrap `yield` in a catch block).
    /// </summary>
    private async Task<bool> MoveNextTranslated<T>(IAsyncStreamReader<T> reader, CancellationToken cancellationToken)
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

    /// <summary>
    /// Initializes transaction reference.
    /// </summary>
    private EvitaClientTransaction CreateAndInitTransaction()
    {
        if (!_sessionTraits.IsReadWrite())
        {
            throw new TransactionNotSupportedException("Transaction cannot be opened in read only session!");
        }

        if (CatalogState == CatalogState.WarmingUp)
        {
            throw new TransactionNotSupportedException("Catalog " + CatalogName +
                                                       " doesn't support transactions yet. Call `goLiveAndClose()` method first!");
        }

        var grpcResponse = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetTransactionId(new Empty())
        );

        var tx = new EvitaClientTransaction(EvitaDataTypesConverter.ToGuid(grpcResponse.TransactionId),
            grpcResponse.CatalogVersion);
        _transactionAccessor.GetAndSet(transaction =>
        {
            Assert.IsPremiseValid(transaction == null, "Transaction unexpectedly found!");
            if (_sessionTraits.IsDryRun())
            {
                tx.SetRollbackOnly();
            }

            return tx;
        });
        return tx;
    }

    /// <summary>
    /// Executes passed lambda in existing transaction or throws exception.
    /// </summary>
    /// <param name="logic">logic to apply</param>
    /// <typeparam name="T">return type of the passed logic</typeparam>
    /// <exception cref="UnexpectedTransactionStateException">if transaction is not open</exception>
    /// <returns>result of passed logic</returns>
    private T ExecuteInTransactionIfPossible<T>(Func<EvitaClientSession, T> logic)
    {
        if (_transactionAccessor.Value == null && CatalogState == CatalogState.Alive)
        {
            using var newTransaction = CreateAndInitTransaction();
            try
            {
                return logic.Invoke(this);
            }
            catch (Exception ex)
            {
                _transactionAccessor.Value?.SetRollbackOnly();
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        // the transaction might already exist
        try
        {
            return logic.Invoke(this);
        }
        catch (Exception ex)
        {
            _transactionAccessor.Value?.SetRollbackOnly();
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Async counterpart of <see cref="ExecuteInTransactionIfPossible{T}"/>.
    /// </summary>
    private async Task<T> ExecuteInTransactionIfPossibleAsync<T>(Func<EvitaClientSession, Task<T>> logic)
    {
        if (_transactionAccessor.Value == null && CatalogState == CatalogState.Alive)
        {
            using var newTransaction = CreateAndInitTransaction();
            try
            {
                return await logic.Invoke(this).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _transactionAccessor.Value?.SetRollbackOnly();
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        // the transaction might already exist
        try
        {
            return await logic.Invoke(this).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _transactionAccessor.Value?.SetRollbackOnly();
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> and returns zero or exactly one entity result. Method
    /// behaves exactly the same as <see cref="Query{T, TS}(Query)"/> but verifies the count of returned results and
    /// translates it to simplified return type.
    /// 
    /// Because result is generic and may contain different data as its contents (based on input query), additional
    /// parameter `expectedType` is passed. This parameter allows to check whether passed response contains the expected
    /// type of data before returning it back to the client. This should prevent late ClassCastExceptions on the client
    /// side.
    /// </summary>
    /// <param name="query">query to process</param>
    /// <returns>a computed response</returns>
    /// <exception cref="EvitaInvalidUsageException">thrown when invalid query was passed</exception>
    public EntityReference? QueryOneEntityReference(Query query)
    {
        return QueryOne<EntityReference>(query);
    }

    /// <summary>
    /// Async variant of <see cref="QueryOneEntityReference"/>.
    /// </summary>
    public Task<EntityReference?> QueryOneEntityReferenceAsync(Query query,
        CancellationToken cancellationToken = default)
    {
        return QueryOneAsync<EntityReference>(query, cancellationToken);
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> and returns zero or exactly one entity result. Method
    /// behaves exactly the same as <see cref="Query{T, TS}(Query)"/> but verifies the count of returned results and
    /// translates it to simplified return type.
    /// 
    /// Because result is generic and may contain different data as its contents (based on input query), additional
    /// parameter `expectedType` is passed. This parameter allows to check whether passed response contains the expected
    /// type of data before returning it back to the client. This should prevent late ClassCastExceptions on the client
    /// side.
    /// </summary>
    /// <param name="query">query to process</param>
    /// <returns>a computed response</returns>
    /// <exception cref="EvitaInvalidUsageException">thrown when invalid query was passed</exception>
    public ISealedEntity? QueryOneSealedEntity(Query query)
    {
        return QueryOne<ISealedEntity>(query);
    }

    /// <summary>
    /// Async variant of <see cref="QueryOneSealedEntity"/>.
    /// </summary>
    public Task<ISealedEntity?> QueryOneSealedEntityAsync(Query query,
        CancellationToken cancellationToken = default)
    {
        return QueryOneAsync<ISealedEntity>(query, cancellationToken);
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> and returns simplified list of results. Method
    /// behaves exactly the same as  but verifies the count of returned results and
    /// translates it to simplified return type. This method will throw out all possible extra results from, because there is
    /// no way how to propagate them in return value. If you require extra results or paginated list use
    /// the <see cref="Query{T, TS}(Query)"/> method.
    /// 
    /// Because result is generic and may contain different data as its contents (based on input query), additional
    /// parameter `expectedType` is passed. This parameter allows to check whether passed response contains the expected
    /// type of data before returning it back to the client. This should prevent late ClassCastExceptions on the client
    /// side.
    /// </summary>
    /// <param name="query">query to process</param>
    /// <returns>a computed response</returns>
    /// <exception cref="EvitaInvalidUsageException">thrown when invalid query was passed</exception>
    public IList<EntityReference> QueryListOfEntityReferences(Query query)
    {
        return QueryList<EntityReference>(query);
    }

    /// <summary>
    /// Async variant of <see cref="QueryListOfEntityReferences"/>.
    /// </summary>
    public Task<IList<EntityReference>> QueryListOfEntityReferencesAsync(Query query,
        CancellationToken cancellationToken = default)
    {
        return QueryListAsync<EntityReference>(query, cancellationToken);
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> and returns simplified list of results. Method
    /// behaves exactly the same as  but verifies the count of returned results and
    /// translates it to simplified return type. This method will throw out all possible extra results from, because there is
    /// no way how to propagate them in return value. If you require extra results or paginated list use
    /// the <see cref="Query{T, TS}(Query)"/> method.
    /// 
    /// Because result is generic and may contain different data as its contents (based on input query), additional
    /// parameter `expectedType` is passed. This parameter allows to check whether passed response contains the expected
    /// type of data before returning it back to the client. This should prevent late ClassCastExceptions on the client
    /// side.
    /// </summary>
    /// <param name="query">query to process</param>
    /// <returns>a computed response</returns>
    /// <exception cref="EvitaInvalidUsageException">thrown when invalid query was passed</exception>
    public IList<ISealedEntity> QueryListOfSealedEntities(Query query)
    {
        return QueryList<ISealedEntity>(EnsureEntityFetchPresent(query));
    }

    /// <summary>
    /// Async variant of <see cref="QueryListOfSealedEntities"/>.
    /// </summary>
    public Task<IList<ISealedEntity>> QueryListOfSealedEntitiesAsync(Query query,
        CancellationToken cancellationToken = default)
    {
        return QueryListAsync<ISealedEntity>(EnsureEntityFetchPresent(query), cancellationToken);
    }

    /// <summary>
    /// Asserts if the request makes sense. This method is used to prevent invalid usage of the API.
    /// This is a basic check that is performed on the client side to unnecessary calls to the server.
    /// It verified expected types and requested types and throws exception if they don't match.
    /// </summary>
    private static void AssertRequestMakesSense<T>(Query query) where T : IEntityClassifier
    {
        if (typeof(ISealedEntity).IsAssignableFrom(typeof(T)) &&
            (query.Require == null ||
             FinderVisitor.FindConstraints<IConstraint>(query.Require, x => x is EntityFetch,
                 x => x is ISeparateEntityContentRequireContainer).Count == 0))
            throw new EvitaInvalidUsageException(
                "Method call expects `" + typeof(T).FullName + "` in result, yet it doesn't define `entityFetch` " +
                "in the requirements. This would imply that only entity references " +
                "will be returned by the server!"
            );
    }

    private ISealedEntity? DeleteEntityInternal(string entityType, int primaryKey, IEntityContentRequire[] require)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var stringWithParameters = ToStringWithParameterExtraction(require);
            var grpcResponse = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntity(
                    new GrpcDeleteEntityRequest
                    {
                        EntityType = entityType,
                        PrimaryKey = primaryKey,
                        Require = stringWithParameters.Query,
                        PositionalQueryParams =
                        {
                            stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)
                        }
                    }
                )
            );
            return grpcResponse.Entity is not null
                ? EntityConverter.ToEntity<ISealedEntity>(
                    entity => _schemaCache.GetEntitySchemaOrThrow(
                        entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                    ),
                    grpcResponse.Entity,
                    new EvitaRequest(
                        IQueryConstraints.Query(
                            Collection(entityType),
                            Require(
                                EntityFetch(require)
                            )
                        ),
                        DateTimeOffset.Now
                    )
                )
                : default;
        });
    }

    private DeletedHierarchy<ISealedEntity> DeleteEntityHierarchyInternal(
        string entityType,
        int primaryKey,
        params IEntityContentRequire[] require
    )
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var stringWithParameters = ToStringWithParameterExtraction(require);
            var grpcResponse = ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntityAndItsHierarchy(
                    new GrpcDeleteEntityRequest
                    {
                        EntityType = entityType,
                        PrimaryKey = primaryKey,
                        Require = stringWithParameters.Query,
                        PositionalQueryParams =
                        {
                            stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)
                        }
                    }
                )
            );
            return new DeletedHierarchy<ISealedEntity>(
                grpcResponse.DeletedEntities,
                grpcResponse.DeletedRootEntity is not null
                    ? EntityConverter.ToEntity<ISealedEntity>(
                        entity => _schemaCache.GetEntitySchemaOrThrow(
                            entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                        ),
                        grpcResponse.DeletedRootEntity,
                        new EvitaRequest(
                            IQueryConstraints.Query(
                                Collection(entityType),
                                Require(
                                    EntityFetch(require)
                                )
                            ),
                            DateTimeOffset.Now
                        )
                    )
                    : null
            );
        });
    }

    /// <summary>
    /// Assert that checks if the session is active. If not, it throws <see cref="InstanceTerminatedException"/>.
    /// </summary>
    /// <exception cref="InstanceTerminatedException">thrown when this session is not active</exception>
    private void AssertActive()
    {
        if (Active)
        {
            _lastCall = Environment.TickCount;
        }
        else
        {
            throw new InstanceTerminatedException("session");
        }
    }

    /// <summary>
    /// Terminates Evita session and releases all used resources. This method renders the session unusable and any further
    /// calls to this session should end up with <see cref="InstanceTerminatedException"/>
    /// 
    /// This method is idempotent and may be called multiple times. Only first call is really processed and others are
    /// ignored.
    /// </summary>
    public void Dispose()
    {
        if (Active)
        {
            try
            {
                ExecuteWithBlockingEvitaSessionService(evitaSessionService =>
                    evitaSessionService.Close(new GrpcCloseRequest
                    {
                        CommitBehaviour = EvitaEnumConverter.ToGrpcCommitBehavior(CommitBehavior),
                        CatalogName = CatalogName
                    }));
            }
            catch (EvitaInternalError e)
            {
                // Dispose must not throw - the session may have been invalidated on the server side; use Close()
                // directly when the commit confirmation matters
                Console.WriteLine($"Session {SessionId} close failed during dispose: {e.Message}");
            }
            catch (EvitaInvalidUsageException e)
            {
                // ditto
                Console.WriteLine($"Session {SessionId} close failed during dispose: {e.Message}");
            }
        }

        CloseInternally();
    }

    [GeneratedRegex("(\\w+:\\w+:\\w+): (.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();

    private class ClientEntitySchemaAccessor(EvitaClientSession session) : IEntitySchemaProvider
    {
        public IEnumerable<IEntitySchema?> GetEntitySchemas()
        {
            return (
                    session.Active
                        ? session.GetAllEntityTypes()
                        : session.Client.QueryCatalog(
                            session.CatalogName,
                            x => x.GetAllEntityTypes()
                        )
                )
                .Select(GetEntitySchema)
                .Where(x => x is not null);
        }

        public IEntitySchema? GetEntitySchema(string entityType)
        {
            return session.Active
                ? session.GetEntitySchema(entityType)
                : session.Client.QueryCatalog(
                    session.CatalogName,
                    x => x.GetEntitySchema(entityType));
        }
    }
}
