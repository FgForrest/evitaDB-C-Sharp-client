using System.Text.RegularExpressions;
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
using EvitaDB.Client.Session;
using EvitaDB.Client.Utils;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using static EvitaDB.Client.Queries.Visitor.PrettyPrintingVisitor;
using static EvitaDB.Client.Queries.IQueryConstraints;
using StatusCode = Grpc.Core.StatusCode;

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
    public string CatalogName { get; }
    public CatalogState CatalogState { get; }
    public Guid SessionId { get; }
    private readonly EvitaEntitySchemaCache _schemaCache;
    private readonly SessionTraits _sessionTraits;
    private readonly Action<EvitaClientSession> _onTerminationCallback;
    private readonly AtomicReference<EvitaClientTransaction> _transactionAccessor = new();

    private static readonly Regex ErrorMessagePattern = MyRegex();

    public bool Active { get; private set; } = true;
    private long _lastCall;

    private readonly string _clientId;

    public EvitaClientSession(EvitaClient evitaClient, EvitaEntitySchemaCache schemaCache, ChannelPool channelPool,
        string catalogName,
        CatalogState catalogState, Guid sessionId, SessionTraits sessionTraits,
        Action<EvitaClientSession> onTerminationCallback)
    {
        _schemaCache = schemaCache;
        _channelPool = channelPool;
        CatalogName = catalogName;
        CatalogState = catalogState;
        SessionId = sessionId;
        _sessionTraits = sessionTraits;
        _onTerminationCallback = onTerminationCallback;
        _clientId = evitaClient.Configuration.ClientId;
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

            var response = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DefineEntitySchema(request)
            );

            var theSchema = EntitySchemaConverter.Convert(response.EntitySchema);
            _schemaCache.SetLatestEntitySchema(theSchema);
            return new EntitySchemaDecorator(GetCatalogSchema, theSchema);
        });
        return newEntitySchema.OpenForWrite();
    }

    /// <summary>
    ///  Method that is called within the <see cref="EvitaClientSession"/> to apply the wanted logic on a channel retrieved
    ///  from a channel pool.
    /// </summary>
    /// <param name="evitaSessionServiceClient">function that holds a logic passed by the caller</param>
    /// <typeparam name="T">return type of the function</typeparam>
    /// <returns>result of the applied function</returns>
    /// <exception cref="InstanceTerminatedException">thrown when no session has been passed to the server when one is required</exception>
    /// <exception cref="EvitaInvalidUsageException">error caused by invalid operations executed by the programmer</exception>
    /// <exception cref="EvitaInternalError">error caused by internal error in the database</exception>
    private T ExecuteWithEvitaSessionService<T>(
        Func<EvitaSessionService.EvitaSessionServiceClient, T> evitaSessionServiceClient)
    {
        var channel = _channelPool.GetChannel();
        try
        {
            SessionIdHolder.SetSessionId(CatalogName, SessionId.ToString());
            return evitaSessionServiceClient.Invoke(
                new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker));
        }
        catch (RpcException rpcException)
        {
            var statusCode = rpcException.StatusCode;
            var description = rpcException.Status.Detail;
            if (statusCode == StatusCode.Unauthenticated)
            {
                // close session and rethrow
                CloseInternally();
                throw new InstanceTerminatedException("session");
            }
            else if (statusCode == StatusCode.InvalidArgument)
            {
                var expectedFormat = ErrorMessagePattern.Match(description);
                if (expectedFormat.Success)
                {
                    throw EvitaInvalidUsageException.CreateExceptionWithErrorCode(
                        expectedFormat.Groups[2].ToString(), expectedFormat.Groups[1].ToString()
                    );
                }
                else
                {
                    throw new EvitaInvalidUsageException(description);
                }
            }
            else
            {
                var expectedFormat = ErrorMessagePattern.Match(description);
                if (expectedFormat.Success)
                {
                    throw EvitaInternalError.CreateExceptionWithErrorCode(
                        expectedFormat.Groups[2].ToString(), expectedFormat.Groups[1].ToString()
                    );
                }
                else
                {
                    throw new EvitaInternalError(description);
                }
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
        AssertActive();
        var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetAllEntityTypes(new Empty())
        );
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
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        var stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = { stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam) }
        };
        var grpcResponse = ExecuteWithEvitaSessionService(session => session.QueryOne(request));

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
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        var stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = { stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam) }
        };

        var grpcResponse = ExecuteWithEvitaSessionService(session => session.QueryList(request));

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
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        var stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = { stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam) }
        };
        var grpcResponse = ExecuteWithEvitaSessionService(session => session.Query(request));
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
    /// for creation use <see cref="IQueryConstraints.Query(IQueryConstraints.Collection,IQueryConstraints.FilterBy,IQueryConstraints.OrderBy,IQueryConstraints.Require)"/> or similar methods
    /// for defining constraint use {@link QueryConstraints} static methods</param>
    /// <returns>full response data transfer object with all available data</returns>
    /// <seealso cref="IQueryConstraints"/>
    public EvitaResponse<ISealedEntity> QuerySealedEntity(Query query)
    {
        if (query.Require == null)
        {
            return Query<EvitaEntityResponse, ISealedEntity>(
                IQueryConstraints.Query(
                    query.Collection,
                    query.FilterBy,
                    query.OrderBy,
                    Require(EntityFetch())
                )
            );
        }

        if (FinderVisitor.FindConstraints<IConstraint>(query.Require, x => x is EntityFetch,
                x => x is ISeparateEntityContentRequireContainer).Count == 0)
        {
            return Query<EvitaEntityResponse, ISealedEntity>(
                IQueryConstraints.Query(
                    query.Collection,
                    query.FilterBy,
                    query.OrderBy,
                    (Require)query.Require.GetCopyWithNewChildren(
                        new IRequireConstraint?[] { Require(EntityFetch()) }
                            .Concat(query.Require.Children).ToArray(),
                        query.Require.AdditionalChildren
                    )
                )
            );
        }

        return Query<EvitaEntityResponse, ISealedEntity>(query);
    }

    /// <summary>
    /// Method executes query on <see cref="ICatalog"/> data and returns result.
    /// </summary>
    /// <param name="query">input query,
    /// for creation use <see cref="IQueryConstraints.Query(IQueryConstraints.Collection,IQueryConstraints.FilterBy,IQueryConstraints.OrderBy,IQueryConstraints.Require)"/> or similar methods
    /// for defining constraint use {@link QueryConstraints} static methods</param>
    /// <returns>response data transfer object only primary keys and and entity types included</returns>
    /// <seealso cref="IQueryConstraints"/>
    public EvitaResponse<EntityReference> QueryEntityReference(Query query)
    {
        return Query<EvitaEntityReferenceResponse, EntityReference>(query);
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
        var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
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
        AssertActive();

        var stringWithParameters = ToStringWithParameterExtraction(require);
        var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetEntity(
                new GrpcEntityRequest
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
            : null;
    }

    /// <summary>
    /// Method returns count of all entities stored in the collection of passed entity type.
    /// </summary>
    public int GetEntityCollectionSize(string entityType)
    {
        AssertActive();
        var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetEntityCollectionSize(
                new GrpcEntityCollectionSizeRequest { EntityType = entityType }
            )
        );
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

            var response = ExecuteWithEvitaSessionService(evitaSessionService =>
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
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                    evitaSessionService.DeleteCollection(new GrpcDeleteCollectionRequest { EntityType = entityType }
                    )
                );
                _schemaCache.RemoveLatestEntitySchema(entityType);
                return grpcResponse.Deleted;
            }
        );
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
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntityAndItsHierarchy(
                    new GrpcDeleteEntityRequest { EntityType = entityType, PrimaryKey = primaryKey }
                )
            );
            return grpcResponse.DeletedEntities;
        });
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
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntity(
                    new GrpcDeleteEntityRequest { EntityType = entityType, PrimaryKey = primaryKey }
                )
            );
            return grpcResponse.Entity is not null || grpcResponse.EntityReference is not null;
        });
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
            var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
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
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var stringWithParameters = ToStringWithParameterExtraction(query);
            var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
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
            return grpcResponse.DeletedEntities;
        });
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
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                    evitaSessionService.RenameCollection(
                        new GrpcRenameCollectionRequest { EntityType = entityType, NewName = newName }
                    )
                );
                _schemaCache.RemoveLatestEntitySchema(entityType);
                return grpcResponse.Renamed;
            }
        );
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
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                    evitaSessionService.ReplaceCollection(
                        new GrpcReplaceCollectionRequest
                        {
                            EntityTypeToBeReplaced = entityTypeToBeReplaced,
                            EntityTypeToBeReplacedWith = entityTypeToBeReplacedWith
                        }
                    )
                );
                _schemaCache.RemoveLatestEntitySchema(entityTypeToBeReplaced);
                _schemaCache.RemoveLatestEntitySchema(entityTypeToBeReplacedWith);
                return grpcResponse.Replaced;
            }
        );
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
            var response = ExecuteWithEvitaSessionService(evitaSessionService =>
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

            var response = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateAndFetchEntitySchema(request)
            );

            var updatedSchema = EntitySchemaConverter.Convert(response.EntitySchema);
            _schemaCache.AnalyzeMutations(schemaMutation);
            _schemaCache.SetLatestEntitySchema(updatedSchema);
            return new EntitySchemaDecorator(GetCatalogSchema, updatedSchema);
        });
    }

    /// <summary>
    /// Terminates opened transaction - either by rollback or commit depending on <see cref="EvitaClientTransaction.RollbackOnly"/>.
    /// This method throws exception only when transaction hasn't been opened.
    /// </summary>
    public void CloseTransaction()
    {
        AssertActive();
        var transaction = _transactionAccessor.Value;
        if (transaction is null)
            throw new UnexpectedTransactionStateException("No transaction has been opened!");
        DestroyTransaction();
        transaction.Close();
    }

    /// <summary>
    /// Destroys transaction reference.
    /// </summary>
    private void DestroyTransaction()
    {
        _transactionAccessor.GetAndSet(transaction =>
        {
            Assert.IsTrue(transaction is not null, "Transaction unexpectedly not present!");
            ExecuteWithEvitaSessionService(session =>
            {
                session.CloseTransaction(new GrpcCloseTransactionRequest { Rollback = transaction!.RollbackOnly });
                return true;
            });
            return null;
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
    public bool GoLiveAndClose()
    {
        AssertActive();
        var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GoLiveAndClose(new Empty())
        );
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
        if (Active)
        {
            ExecuteWithEvitaSessionService(session =>
            {
                session.Close(new Empty());
                return true;
            });
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

            var response = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateAndFetchCatalogSchema(request)
            );

            var updatedCatalogSchema =
                CatalogSchemaConverter.Convert(GetEntitySchemaOrThrow, response.CatalogSchema);
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
            : Array.Empty<IEvitaResponseExtraResult>();
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
        var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetCatalogSchema(new Empty())
        );
        return CatalogSchemaConverter.Convert(
            GetEntitySchemaOrThrow, grpcResponse.CatalogSchema
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
    /// Method inserts to or updates entity in collection according to passed set of mutations.
    /// </summary>
    /// <param name="entityMutation">list of mutation snippets that alter or form the entity</param>
    public EntityReference UpsertEntity(IEntityMutation entityMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            var grpcEntityMutation = EntityMutationConverter.Convert(entityMutation);
            var grpcResult = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpsertEntity(
                    new GrpcUpsertEntityRequest { EntityMutation = grpcEntityMutation }
                )
            );
            var grpcReference = grpcResult.EntityReference;
            return new EntityReference(
                grpcReference.EntityType, grpcReference.PrimaryKey
            );
        });
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
            var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
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

        var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.OpenTransaction(new Empty())
        );

        var tx = new EvitaClientTransaction(this, grpcResponse.TransactionId);
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
        if (query.Require == null)
        {
            return QueryList<ISealedEntity>(
                IQueryConstraints.Query(
                    query.Collection,
                    query.FilterBy,
                    query.OrderBy,
                    Require(EntityFetch())
                )
            );
        }

        if (FinderVisitor.FindConstraints<IConstraint>(query.Require, x => x is EntityFetch,
                y => y is ISeparateEntityContentRequireContainer).Count == 0)
        {
            return QueryList<ISealedEntity>(
                IQueryConstraints.Query(
                    query.Collection,
                    query.FilterBy,
                    query.OrderBy,
                    (Require)query.Require.GetCopyWithNewChildren(
                        new IRequireConstraint?[] { Require(EntityFetch()) }.Concat(query.Require.Children).ToArray(),
                        query.Require.AdditionalChildren
                    )
                )
            );
        }

        return QueryList<ISealedEntity>(query);
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
            var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
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
            var grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
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
            ExecuteWithEvitaSessionService(evitaSessionService => evitaSessionService.Close(new Empty()));
        }

        CloseInternally();
    }

    [GeneratedRegex("(\\w+:\\w+:\\w+): (.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();
}
