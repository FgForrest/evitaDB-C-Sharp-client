using System.Text.RegularExpressions;
using EvitaDB.Client.Converters.Models;
using EvitaDB.Client.Converters.Models.Data;
using EvitaDB.Client.Converters.Models.Data.Mutations;
using EvitaDB.Client.Converters.Models.Schema;
using EvitaDB.Client.Converters.Models.Schema.Mutations;
using EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;
using EvitaDB.Client.DataTypes;
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

namespace EvitaDB.Client;

public class EvitaClientSession : IClientContext, IDisposable
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

    private static readonly Regex ErrorMessagePattern = new(
        "(\\w+:\\w+:\\w+): (.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

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

    public IEntitySchemaBuilder DefineEntitySchema(string entityType)
    {
        AssertActive();
        ISealedEntitySchema newEntitySchema = ExecuteInTransactionIfPossible(_ =>
        {
            GrpcDefineEntitySchemaRequest request = new GrpcDefineEntitySchemaRequest
            {
                EntityType = entityType
            };

            GrpcDefineEntitySchemaResponse response = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DefineEntitySchema(request)
            );

            EntitySchema theSchema = EntitySchemaConverter.Convert(response.EntitySchema);
            _schemaCache.SetLatestEntitySchema(theSchema);
            return new EntitySchemaDecorator(GetCatalogSchema, theSchema);
        });
        return newEntitySchema.OpenForWrite();
    }

    private T ExecuteWithEvitaSessionService<T>(
        Func<EvitaSessionService.EvitaSessionServiceClient, T> evitaSessionServiceClient)
    {
        IClientContext clientContext = this;
        return clientContext.ExecuteWithClientId(
            _clientId,
            () =>
            {
                ChannelInvoker channel = _channelPool.GetChannel();
                try
                {
                    SessionIdHolder.SetSessionId(CatalogName, SessionId.ToString());
                    return evitaSessionServiceClient.Invoke(
                        new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker));
                }
                catch (RpcException rpcException)
                {
                    StatusCode statusCode = rpcException.StatusCode;
                    string description = rpcException.Status.Detail;
                    if (statusCode == StatusCode.Unauthenticated)
                    {
                        // close session and rethrow
                        CloseInternally();
                        throw new InstanceTerminatedException("session");
                    }
                    else if (statusCode == StatusCode.InvalidArgument)
                    {
                        Match expectedFormat = ErrorMessagePattern.Match(description);
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
                        Match expectedFormat = ErrorMessagePattern.Match(description);
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
            });
    }

    public T Execute<T>(Func<EvitaClientSession, T> logic)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(logic);
    }

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

    public ISet<string> GetAllEntityTypes()
    {
        AssertActive();
        GrpcEntityTypesResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetAllEntityTypes(new Empty())
        );
        return new HashSet<string>(grpcResponse.EntityTypes);
    }

    public TS? QueryOne<TS>(Query query) where TS : class, IEntityClassifier
    {
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        StringWithParameters stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
        };
        GrpcQueryOneResponse grpcResponse = ExecuteWithEvitaSessionService(session => session.QueryOne(request));

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

    public IList<TS> QueryList<TS>(Query query) where TS : IEntityClassifier
    {
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        StringWithParameters stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
        };
        GrpcQueryListResponse grpcResponse = ExecuteWithEvitaSessionService(session => session.QueryList(request));

        if (typeof(IEntityReference).IsAssignableFrom(typeof(TS)))
        {
            return (IList<TS>) EntityConverter.ToEntityReferences(grpcResponse.EntityReferences);
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

    public T Query<T, TS>(Query query) where TS : IEntityClassifier where T : EvitaResponse<TS>
    {
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        StringWithParameters stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
        };
        GrpcQueryResponse grpcResponse = ExecuteWithEvitaSessionService(session => session.Query(request));
        IEvitaResponseExtraResult[] extraResults = GetEvitaResponseExtraResults(
            grpcResponse,
            new EvitaRequest(query, DateTimeOffset.Now)
        );

        if (typeof(IEntityReference).IsAssignableFrom(typeof(TS)))
        {
            IDataChunk<EntityReference> recordPage = ResponseConverter.ConvertToDataChunk<EntityReference>(
                grpcResponse,
                grpcRecordPage => EntityConverter.ToEntityReferences(grpcRecordPage.EntityReferences)
            );
            return (new EvitaEntityReferenceResponse(query, recordPage, extraResults) as T)!;
        }

        if (typeof(ISealedEntity).IsAssignableFrom(typeof(TS)))
        {
            IDataChunk<ISealedEntity> recordPage = ResponseConverter.ConvertToDataChunk(
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

    public EvitaResponse<ISealedEntity> QuerySealedEntity(Query query)
    {
        if (query.Require == null)
        {
            return Query<EvitaEntityResponse, ISealedEntity>(
                IQueryConstraints.Query(
                    query.Entities,
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
                    query.Entities,
                    query.FilterBy,
                    query.OrderBy,
                    (Require) query.Require.GetCopyWithNewChildren(
                        new IRequireConstraint?[] {Require(EntityFetch())}
                            .Concat(query.Require.Children).ToArray(),
                        query.Require.AdditionalChildren
                    )
                )
            );
        }

        return Query<EvitaEntityResponse, ISealedEntity>(query);
    }

    public EvitaResponse<EntityReference> QueryEntityReference(Query query)
    {
        return Query<EvitaEntityReferenceResponse, EntityReference>(query);
    }

    public ISealedEntitySchema UpdateAndFetchEntitySchema(IEntitySchemaBuilder entitySchemaBuilder)
    {
        ModifyEntitySchemaMutation? schemaMutation = entitySchemaBuilder.ToMutation();
        if (schemaMutation is not null)
        {
            return UpdateAndFetchEntitySchema(schemaMutation);
        }

        return GetEntitySchemaOrThrow(entitySchemaBuilder.Name);
    }

    private EntitySchema? FetchEntitySchema(string entityType)
    {
        GrpcEntitySchemaResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetEntitySchema(new GrpcEntitySchemaRequest {EntityType = entityType})
        );
        if (grpcResponse.EntitySchema is null)
        {
            return null;
        }

        return EntitySchemaConverter.Convert(grpcResponse.EntitySchema);
    }

    public ISealedEntity? GetEntity(string entityType, int primaryKey,
        params IEntityContentRequire[] require)
    {
        AssertActive();

        StringWithParameters stringWithParameters = ToStringWithParameterExtraction(require);
        GrpcEntityResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetEntity(
                new GrpcEntityRequest
                {
                    EntityType = entityType,
                    PrimaryKey = primaryKey,
                    Require = stringWithParameters.Query,
                    PositionalQueryParams = {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
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

    public int GetEntityCollectionSize(string entityType)
    {
        AssertActive();
        GrpcEntityCollectionSizeResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetEntityCollectionSize(
                new GrpcEntityCollectionSizeRequest
                {
                    EntityType = entityType
                }
            )
        );
        return grpcResponse.Size;
    }

    public int UpdateCatalogSchema(params ILocalCatalogSchemaMutation[] schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            List<GrpcLocalCatalogSchemaMutation>
                grpcSchemaMutations = schemaMutation
                    .Select(CatalogSchemaMutationConverter.Convert)
                    .ToList();

            GrpcUpdateCatalogSchemaRequest request = new GrpcUpdateCatalogSchemaRequest
            {
                SchemaMutations = {grpcSchemaMutations}
            };

            GrpcUpdateCatalogSchemaResponse response = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateCatalogSchema(request)
            );

            _schemaCache.AnalyzeMutations(schemaMutation);
            return response.Version;
        });
    }

    public bool DeleteCollection(string entityType)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                GrpcDeleteCollectionResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                    evitaSessionService.DeleteCollection(new GrpcDeleteCollectionRequest
                        {
                            EntityType = entityType
                        }
                    )
                );
                _schemaCache.RemoveLatestEntitySchema(entityType);
                return grpcResponse.Deleted;
            }
        );
    }

    public int DeleteEntityAndItsHierarchy(string entityType, int primaryKey)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            GrpcDeleteEntityAndItsHierarchyResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntityAndItsHierarchy(
                    new GrpcDeleteEntityRequest
                    {
                        EntityType = entityType,
                        PrimaryKey = primaryKey
                    }
                )
            );
            return grpcResponse.DeletedEntities;
        });
    }

    public DeletedHierarchy<ISealedEntity> DeleteEntityAndItsHierarchy(string entityType, int primaryKey,
        params IEntityContentRequire[] require)
    {
        return DeleteEntityHierarchyInternal(entityType, primaryKey, require);
    }

    public bool DeleteEntity(string entityType, int primaryKey)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            GrpcDeleteEntityResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntity(
                    new GrpcDeleteEntityRequest
                    {
                        EntityType = entityType,
                        PrimaryKey = primaryKey
                    }
                )
            );
            return grpcResponse.Entity is not null || grpcResponse.EntityReference is not null;
        });
    }

    public ISealedEntity[] DeleteSealedEntitiesAndReturnBodies(Query query)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            EvitaRequest evitaRequest = new EvitaRequest(
                query,
                DateTimeOffset.Now,
                typeof(ISealedEntity)
            );
            StringWithParameters stringWithParameters = query.ToStringWithParametersExtraction();
            GrpcDeleteEntitiesResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntities(
                    new GrpcDeleteEntitiesRequest
                    {
                        Query = stringWithParameters.Query,
                        PositionalQueryParams =
                            {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
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

    public ISealedEntity? DeleteEntity(string entityType, int primaryKey, params IEntityContentRequire[] require)
    {
        return DeleteEntityInternal(entityType, primaryKey, require);
    }


    public int DeleteEntities(Query query)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            StringWithParameters stringWithParameters = ToStringWithParameterExtraction(query);
            GrpcDeleteEntitiesResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntities(
                    new GrpcDeleteEntitiesRequest
                    {
                        Query = stringWithParameters.Query,
                        PositionalQueryParams =
                            {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
                    }
                )
            );
            return grpcResponse.DeletedEntities;
        });
    }

    public bool RenameCollection(string entityType, string newName)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                GrpcRenameCollectionResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                    evitaSessionService.RenameCollection(
                        new GrpcRenameCollectionRequest
                        {
                            EntityType = entityType,
                            NewName = newName
                        }
                    )
                );
                _schemaCache.RemoveLatestEntitySchema(entityType);
                return grpcResponse.Renamed;
            }
        );
    }

    public bool ReplaceCollection(string entityTypeToBeReplaced, string entityTypeToBeReplacedWith)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                GrpcReplaceCollectionResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
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

    public int UpdateEntitySchema(ModifyEntitySchemaMutation schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            GrpcModifyEntitySchemaMutation grpcSchemaMutation =
                ModifyEntitySchemaMutationConverter.Convert(schemaMutation);
            GrpcUpdateEntitySchemaRequest request = new GrpcUpdateEntitySchemaRequest
            {
                SchemaMutation = grpcSchemaMutation
            };
            GrpcUpdateEntitySchemaResponse response = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateEntitySchema(request)
            );
            _schemaCache.AnalyzeMutations(schemaMutation);
            return response.Version;
        });
    }

    public int UpdateEntitySchema(IEntitySchemaBuilder entitySchemaBuilder)
    {
        ModifyEntitySchemaMutation? mutation = entitySchemaBuilder.ToMutation();
        return mutation is not null
            ? UpdateEntitySchema(mutation)
            : GetEntitySchemaOrThrow(entitySchemaBuilder.Name).Version;
    }

    public ISealedEntitySchema UpdateAndFetchEntitySchema(ModifyEntitySchemaMutation schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            GrpcModifyEntitySchemaMutation grpcSchemaMutation =
                ModifyEntitySchemaMutationConverter.Convert(schemaMutation);
            GrpcUpdateEntitySchemaRequest request = new GrpcUpdateEntitySchemaRequest
            {
                SchemaMutation = grpcSchemaMutation
            };

            GrpcUpdateAndFetchEntitySchemaResponse response = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateAndFetchEntitySchema(request)
            );

            EntitySchema updatedSchema = EntitySchemaConverter.Convert(response.EntitySchema);
            _schemaCache.AnalyzeMutations(schemaMutation);
            _schemaCache.SetLatestEntitySchema(updatedSchema);
            return new EntitySchemaDecorator(GetCatalogSchema, updatedSchema);
        });
    }

    public void CloseTransaction()
    {
        AssertActive();
        EvitaClientTransaction? transaction = _transactionAccessor.Value;
        if (transaction is null)
            throw new UnexpectedTransactionStateException("No transaction has been opened!");
        DestroyTransaction();
        transaction.Close();
    }

    private void DestroyTransaction()
    {
        _transactionAccessor.GetAndSet(transaction =>
        {
            Assert.IsTrue(transaction is not null, "Transaction unexpectedly not present!");
            ExecuteWithEvitaSessionService(session =>
            {
                session.CloseTransaction(new GrpcCloseTransactionRequest {Rollback = transaction!.RollbackOnly});
                return true;
            });
            return null;
        });
    }

    public bool GoLiveAndClose()
    {
        AssertActive();
        GrpcGoLiveAndCloseResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GoLiveAndClose(new Empty())
        );
        bool success = grpcResponse.Success;
        if (success)
        {
            CloseInternally();
        }

        return success;
    }

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

    private void CloseInternally()
    {
        if (!Active) return;
        Active = false;
        _onTerminationCallback.Invoke(this);
    }

    public ISealedCatalogSchema UpdateAndFetchCatalogSchema(params ILocalCatalogSchemaMutation[] schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            List<GrpcLocalCatalogSchemaMutation> grpcSchemaMutations = schemaMutation
                .Select(CatalogSchemaMutationConverter.Convert)
                .ToList();

            GrpcUpdateCatalogSchemaRequest request = new GrpcUpdateCatalogSchemaRequest
            {
                SchemaMutations = {grpcSchemaMutations}
            };

            GrpcUpdateAndFetchCatalogSchemaResponse response = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpdateAndFetchCatalogSchema(request)
            );

            CatalogSchema updatedCatalogSchema =
                CatalogSchemaConverter.Convert(GetEntitySchemaOrThrow, response.CatalogSchema);
            ISealedCatalogSchema updatedSchema =
                new CatalogSchemaDecorator(updatedCatalogSchema, GetEntitySchemaOrThrow);
            _schemaCache.AnalyzeMutations(schemaMutation);
            _schemaCache.SetLatestCatalogSchema(updatedCatalogSchema);
            return updatedSchema;
        });
    }

    public ISealedCatalogSchema UpdateAndFetchCatalogSchema(ICatalogSchemaBuilder catalogSchemaBuilder)
    {
        Assert.IsTrue(
            catalogSchemaBuilder.Name.Equals(CatalogName),
            "Schema builder targets `" + catalogSchemaBuilder.Name + "` catalog, but the session targets `" +
            CatalogName + "` catalog!"
        );
        ModifyCatalogSchemaMutation? modifyCatalogSchemaMutation = catalogSchemaBuilder.ToMutation();
        return modifyCatalogSchemaMutation is not null
            ? UpdateAndFetchCatalogSchema(modifyCatalogSchemaMutation.SchemaMutations)
            : GetCatalogSchema();
    }

    public int UpdateCatalogSchema(ICatalogSchemaBuilder catalogSchemaBuilder)
    {
        Assert.IsTrue(
            catalogSchemaBuilder.Name.Equals(CatalogName),
            "Schema builder targets `" + catalogSchemaBuilder.Name + "` catalog, but the session targets `" +
            CatalogName + "` catalog!"
        );
        ModifyCatalogSchemaMutation? modifyCatalogSchemaMutation = catalogSchemaBuilder.ToMutation();
        return modifyCatalogSchemaMutation is not null
            ? UpdateCatalogSchema(modifyCatalogSchemaMutation.SchemaMutations)
            : GetCatalogSchema().Version;
    }

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

    public ISealedCatalogSchema GetCatalogSchema()
    {
        AssertActive();
        return _schemaCache.GetLatestCatalogSchema(FetchCatalogSchema, GetEntitySchema);
    }

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

    private CatalogSchema FetchCatalogSchema()
    {
        GrpcCatalogSchemaResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetCatalogSchema(new Empty())
        );
        return CatalogSchemaConverter.Convert(
            GetEntitySchemaOrThrow, grpcResponse.CatalogSchema
        );
    }

    public ISealedEntitySchema GetEntitySchemaOrThrow(string entityType)
    {
        AssertActive();
        return GetEntitySchema(entityType) ?? throw new CollectionNotFoundException(entityType);
    }

    public ISealedEntitySchema? GetEntitySchema(string entityType)
    {
        AssertActive();
        return _schemaCache.GetLatestEntitySchema(entityType, FetchEntitySchema, GetCatalogSchema);
    }

    public EntityReference UpsertEntity(IEntityBuilder entityBuilder)
    {
        IEntityMutation? mutation = entityBuilder.ToMutation();
        return mutation is not null
            ? UpsertEntity(mutation)
            : new EntityReference(entityBuilder.Type, entityBuilder.PrimaryKey);
    }

    public EntityReference UpsertEntity(IEntityMutation entityMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            GrpcEntityMutation grpcEntityMutation = EntityMutationConverter.Convert(entityMutation);
            GrpcUpsertEntityResponse grpcResult = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpsertEntity(
                    new GrpcUpsertEntityRequest
                    {
                        EntityMutation = grpcEntityMutation
                    }
                )
            );
            GrpcEntityReference grpcReference = grpcResult.EntityReference;
            return new EntityReference(
                grpcReference.EntityType, grpcReference.PrimaryKey
            );
        });
    }

    public ISealedEntity UpsertAndFetchEntity(IEntityBuilder entityBuilder, params IEntityContentRequire[] require)
    {
        IEntityMutation? mutation = entityBuilder.ToMutation();
        return mutation is not null
            ? UpsertAndFetchEntity(mutation, require)
            : GetEntityOrThrow(entityBuilder.Type, entityBuilder.PrimaryKey!.Value, require);
    }

    public ISealedEntity UpsertAndFetchEntity(IEntityMutation entityMutation, params IEntityContentRequire[] require)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(_ =>
        {
            GrpcEntityMutation grpcEntityMutation = EntityMutationConverter.Convert(entityMutation);
            StringWithParameters stringWithParameters = ToStringWithParameterExtraction(require);
            GrpcUpsertEntityResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.UpsertEntity(
                    new GrpcUpsertEntityRequest
                    {
                        EntityMutation = grpcEntityMutation,
                        Require = stringWithParameters.Query,
                        PositionalQueryParams =
                            {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
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

    public ISealedEntity GetEntityOrThrow(string type, int primaryKey, params IEntityContentRequire[] require)
    {
        ISealedEntity? entity = GetEntity(type, primaryKey, require);
        return entity ??
               throw new EvitaInvalidUsageException("Entity `" + type + "` with id `" + primaryKey +
                                                    "` doesn't exist!");
    }

    public IEntityBuilder CreateNewEntity(string entityType)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                IEntitySchema entitySchema;
                if (GetCatalogSchema().CatalogEvolutionModes.Contains(CatalogEvolutionMode.AddingEntityTypes))
                {
                    ISealedEntitySchema? schema = GetEntitySchema(entityType);
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

    public IEntityBuilder CreateNewEntity(string entityType, int primaryKey)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(
            _ =>
            {
                IEntitySchema entitySchema;
                if (GetCatalogSchema().CatalogEvolutionModes.Contains(CatalogEvolutionMode.AddingEntityTypes))
                {
                    ISealedEntitySchema? schema = GetEntitySchema(entityType);
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

        GrpcOpenTransactionResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.OpenTransaction(new Empty())
        );

        EvitaClientTransaction tx = new EvitaClientTransaction(this, grpcResponse.TransactionId);
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

    private T ExecuteInTransactionIfPossible<T>(Func<EvitaClientSession, T> logic)
    {
        if (_transactionAccessor.Value == null && CatalogState == CatalogState.Alive)
        {
            using EvitaClientTransaction newTransaction = CreateAndInitTransaction();
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

    public EntityReference? QueryOneEntityReference(Query query)
    {
        return QueryOne<EntityReference>(query);
    }

    public ISealedEntity? QueryOneSealedEntity(Query query)
    {
        return QueryOne<ISealedEntity>(query);
    }

    public IList<EntityReference> QueryListOfEntityReferences(Query query)
    {
        return QueryList<EntityReference>(query);
    }

    public IList<ISealedEntity> QueryListOfSealedEntities(Query query)
    {
        if (query.Require == null)
        {
            return QueryList<ISealedEntity>(
                IQueryConstraints.Query(
                    query.Entities,
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
                    query.Entities,
                    query.FilterBy,
                    query.OrderBy,
                    (Require) query.Require.GetCopyWithNewChildren(
                        new IRequireConstraint?[] {Require(EntityFetch())}.Concat(query.Require.Children).ToArray(),
                        query.Require.AdditionalChildren
                    )
                )
            );
        }

        return QueryList<ISealedEntity>(query);
    }

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
            StringWithParameters stringWithParameters = ToStringWithParameterExtraction(require);
            GrpcDeleteEntityResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntity(
                    new GrpcDeleteEntityRequest
                    {
                        EntityType = entityType,
                        PrimaryKey = primaryKey,
                        Require = stringWithParameters.Query,
                        PositionalQueryParams =
                            {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
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
            StringWithParameters stringWithParameters = ToStringWithParameterExtraction(require);
            GrpcDeleteEntityAndItsHierarchyResponse grpcResponse = ExecuteWithEvitaSessionService(evitaSessionService =>
                evitaSessionService.DeleteEntityAndItsHierarchy(
                    new GrpcDeleteEntityRequest
                    {
                        EntityType = entityType,
                        PrimaryKey = primaryKey,
                        Require = stringWithParameters.Query,
                        PositionalQueryParams =
                            {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
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

    public void Dispose()
    {
        if (Active)
        {
            ExecuteWithEvitaSessionService(evitaSessionService => evitaSessionService.Close(new Empty()));
        }

        CloseInternally();
    }
}