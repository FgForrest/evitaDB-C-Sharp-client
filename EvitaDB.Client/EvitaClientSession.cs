using EvitaDB.Client.Converters.Models;
using EvitaDB.Client.Converters.Models.Data;
using EvitaDB.Client.Converters.Models.Data.Mutations;
using EvitaDB.Client.Converters.Models.Schema;
using EvitaDB.Client.Converters.Models.Schema.Mutations;
using EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
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
using static EvitaDB.Client.Queries.Visitor.PrettyPrintingVisitor;
using static EvitaDB.Client.Queries.IQueryConstraints;

namespace EvitaDB.Client;

public class EvitaClientSession : IDisposable
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
    private bool _active = true;
    private long _lastCall;

    public EvitaClientSession(EvitaEntitySchemaCache schemaCache, ChannelPool channelPool, string catalogName,
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
    }

    public IEntitySchemaBuilder DefineEntitySchema(string entityType)
    {
        AssertActive();
        ISealedEntitySchema newEntitySchema = ExecuteInTransactionIfPossible(session =>
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
        var channel = _channelPool.GetChannel();
        try
        {
            return evitaSessionServiceClient.Invoke(new EvitaSessionService.EvitaSessionServiceClient(channel.Invoker));
        }
        finally
        {
            _channelPool.ReleaseChannel(channel);
        }
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
                    IQueryConstraints.Require(IQueryConstraints.EntityFetch())
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
                        new IRequireConstraint[] {Require(EntityFetch())}
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
        return ExecuteInTransactionIfPossible(session =>
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
        return ExecuteInTransactionIfPossible(session =>
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

    public bool DeleteEntity(string entityType, int primaryKey)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(session =>
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


    public ISealedEntity? DeleteEntity(string entityType, int primaryKey, params IEntityContentRequire[] require)
    {
        return DeleteEntityInternal(entityType, primaryKey, require);
    }


    public int DeleteEntities(Query query)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(session =>
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
        return ExecuteInTransactionIfPossible(session =>
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
        return ExecuteInTransactionIfPossible(session =>
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
        if (_active)
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
        if (!_active) return;
        _active = false;
        _onTerminationCallback.Invoke(this);
    }

    public ISealedCatalogSchema UpdateAndFetchCatalogSchema(params ILocalCatalogSchemaMutation[] schemaMutation)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(session =>
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
            ISealedCatalogSchema updatedSchema = new CatalogSchemaDecorator(updatedCatalogSchema, GetEntitySchemaOrThrow);
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
        return ExecuteInTransactionIfPossible(session =>
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
            session => new InitialEntityBuilder(GetEntitySchemaOrThrow(entityType), null)
        );
    }

    public IEntityBuilder CreateNewEntity(string entityType, int primaryKey)
    {
        AssertActive();
        return ExecuteInTransactionIfPossible(
            session => new InitialEntityBuilder(GetEntitySchemaOrThrow(entityType), primaryKey)
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

        EvitaClientTransaction? tx = new EvitaClientTransaction(this, grpcResponse.TransactionId);
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
        if (_active)
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
        Close();
    }
}