using Client.Converters.Models;
using Client.Converters.Models.Data;
using Client.Converters.Models.Data.Mutations;
using Client.Converters.Models.Schema;
using Client.Converters.Models.Schema.Mutations;
using Client.Converters.Models.Schema.Mutations.Catalogs;
using Client.DataTypes;
using Client.Exceptions;
using Client.Models;
using Client.Models.Data;
using Client.Models.Data.Mutations;
using Client.Models.Data.Structure;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;
using Client.Models.Schemas.Mutations;
using Client.Models.Schemas.Mutations.Catalogs;
using Client.Pooling;
using Client.Queries;
using Client.Queries.Requires;
using Client.Queries.Visitor;
using Client.Session;
using Client.Utils;
using EvitaDB;
using Google.Protobuf.WellKnownTypes;
using static Client.Queries.Visitor.PrettyPrintingVisitor;

namespace Client;

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
        IEvitaResponseExtraResult[] extraResults = GetEvitaResponseExtraResults(query, grpcResponse);

        if (typeof(EntityReference).IsAssignableFrom(typeof(TS)))
        {
            IDataChunk<EntityReference> recordPage = ResponseConverter.ConvertToDataChunk(
                grpcResponse,
                grpcRecordPage => EntityConverter.ToEntityReferences(grpcRecordPage.EntityReferences)
            );
            return (new EvitaEntityReferenceResponse(query, recordPage, extraResults) as T)!;
        }

        if (typeof(SealedEntity).IsAssignableFrom(typeof(TS)))
        {
            IDataChunk<SealedEntity> recordPage = ResponseConverter.ConvertToDataChunk(
                grpcResponse,
                grpcRecordPage => EntityConverter.ToSealedEntities(
                    grpcRecordPage.SealedEntities.ToList(),
                    (entityType, schemaVersion) => _schemaCache.GetEntitySchemaOrThrow(
                        entityType, schemaVersion, FetchEntitySchema, GetCatalogSchema
                    )
                )
            );
            return (new EvitaEntityResponse(query, recordPage, extraResults) as T)!;
        }

        throw new EvitaInvalidUsageException("Unsupported return type `" + typeof(TS) + "`!");
    }

    public EvitaResponse<SealedEntity> QuerySealedEntity(Query query)
    {
        if (query.Require == null)
        {
            return Query<EvitaEntityResponse, SealedEntity>(
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
            return Query<EvitaEntityResponse, SealedEntity>(
                IQueryConstraints.Query(
                    query.Entities,
                    query.FilterBy,
                    query.OrderBy,
                    (Require) query.Require.GetCopyWithNewChildren(
                        new IRequireConstraint[] {IQueryConstraints.Require(IQueryConstraints.EntityFetch())}
                            .Concat(query.Require.Children).ToArray(),
                        query.Require.AdditionalChildren
                    )
                )
            );
        }
        return Query<EvitaEntityResponse, SealedEntity>(query);
    }

    public EvitaResponse<EntityReference> QueryEntityReference(Query query)
    {
        return Query<EvitaEntityReferenceResponse, EntityReference>(query);
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

    public async Task<T> QueryAsync<T, TS>(Query query) where TS : IEntityClassifier where T : EvitaResponse<TS>
    {
        AssertActive();
        AssertRequestMakesSense<TS>(query);

        StringWithParameters stringWithParameters = query.ToStringWithParametersExtraction();
        var request = new GrpcQueryRequest
        {
            Query = stringWithParameters.Query,
            PositionalQueryParams = {stringWithParameters.Parameters.Select(QueryConverter.ConvertQueryParam)}
        };
        GrpcQueryResponse grpcResponse =
            await ExecuteWithEvitaSessionService(async session => await session.QueryAsync(request));

        IEvitaResponseExtraResult[] extraResults = GetEvitaResponseExtraResults(query, grpcResponse);
        if (typeof(EntityReference).IsAssignableFrom(typeof(TS)))
        {
            IDataChunk<EntityReference> recordPage = ResponseConverter.ConvertToDataChunk(
                grpcResponse,
                grpcRecordPage => EntityConverter.ToEntityReferences(grpcRecordPage.EntityReferences)
            );
            return (new EvitaEntityReferenceResponse(query, recordPage, extraResults) as T)!;
        }

        if (typeof(SealedEntity).IsAssignableFrom(typeof(TS)))
        {
            IDataChunk<SealedEntity> recordPage = ResponseConverter.ConvertToDataChunk(
                grpcResponse,
                grpcRecordPage => EntityConverter.ToSealedEntities(
                    grpcRecordPage.SealedEntities.ToList(),
                    (entityType, schemaVersion) => _schemaCache.GetEntitySchemaOrThrow(
                        entityType, schemaVersion, FetchEntitySchema, GetCatalogSchema
                    )
                )
            );
            return (new EvitaEntityResponse(query, recordPage, extraResults) as T)!;
        }

        throw new EvitaInvalidUsageException("Unsupported return type `" + typeof(TS) + "`!");
    }

    public SealedEntity? GetEntity(string entityType, int primaryKey,
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
            ? EntityConverter.ToSealedEntity(
                entity => _schemaCache.GetEntitySchemaOrThrow(
                    entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                ),
                grpcResponse.Entity
            )
            : null;
    }

    public async Task<SealedEntity?> GetEntityAsync(string entityType, int primaryKey,
        params IEntityContentRequire[] require)
    {
        AssertActive();

        StringWithParameters stringWithParameters = ToStringWithParameterExtraction(require);
        GrpcEntityResponse grpcResponse = await ExecuteWithEvitaSessionService(evitaSessionService =>
            evitaSessionService.GetEntityAsync(
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
            ? EntityConverter.ToSealedEntity(
                entity => _schemaCache.GetEntitySchemaOrThrow(entity.EntityType, entity.SchemaVersion,
                    FetchEntitySchema,
                    GetCatalogSchema),
                grpcResponse.Entity
            )
            : null;
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

    public EntitySchema UpdateAndFetchEntitySchema(ModifyEntitySchemaMutation schemaMutation)
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
            return updatedSchema;
            //return new EntitySchemaDecorator(this::getCatalogSchema, updatedSchema);
            //TODO LATER
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

    public async void CloseAsync()
    {
        if (_active)
        {
            await ExecuteWithEvitaSessionService(async session => { await session.CloseAsync(new Empty()); });
            CloseInternally();
        }
    }

    private void CloseInternally()
    {
        if (!_active) return;
        _active = false;
        _onTerminationCallback.Invoke(this);
    }

    public void Dispose()
    {
        CloseAsync();
    }

    public async Task<int> GetEntityCollectionSize(string entityType)
    {
        AssertActive();
        var grpcResponse = await ExecuteWithEvitaSessionService(async session =>
            await session.GetEntityCollectionSizeAsync(
                new GrpcEntityCollectionSizeRequest
                {
                    EntityType = entityType
                }
            ));
        return grpcResponse.Size;
    }

    public CatalogSchema UpdateAndFetchCatalogSchema(params ILocalCatalogSchemaMutation[] schemaMutation)
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

            CatalogSchema updatedCatalogSchema = CatalogSchemaConverter.Convert(
                GetEntitySchemaOrThrow, response.CatalogSchema
            );
            _schemaCache.AnalyzeMutations(schemaMutation);
            _schemaCache.SetLatestCatalogSchema(updatedCatalogSchema);
            return updatedCatalogSchema;
        });
    }

    private IEvitaResponseExtraResult[] GetEvitaResponseExtraResults(Query query, GrpcQueryResponse grpcResponse)
    {
        return grpcResponse.ExtraResults is not null
            ? ResponseConverter.ToExtraResults(
                sealedEntity => _schemaCache.GetEntitySchemaOrThrow(
                    sealedEntity.EntityType, sealedEntity.SchemaVersion,
                    FetchEntitySchema, GetCatalogSchema
                ),
                query,
                grpcResponse.ExtraResults
            )
            : Array.Empty<IEvitaResponseExtraResult>();
    }

    public CatalogSchema GetCatalogSchema()
    {
        AssertActive();
        return _schemaCache.GetLatestCatalogSchema(FetchCatalogSchema);
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

    public EntitySchema GetEntitySchemaOrThrow(string entityType)
    {
        AssertActive();
        EntitySchema? schema = GetEntitySchema(entityType);
        return schema ?? throw new CollectionNotFoundException(entityType);
    }

    public EntitySchema? GetEntitySchema(string entityType)
    {
        AssertActive();
        return _schemaCache.GetLatestEntitySchema(entityType, FetchEntitySchema, _ => GetCatalogSchema());
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
        return ExecuteInTransactionIfPossible(session =>
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

    public SealedEntity UpsertAndFetchEntity(IEntityBuilder entityBuilder, params IEntityContentRequire[] require)
    {
        IEntityMutation? mutation = entityBuilder.ToMutation();
        return mutation is not null
            ? UpsertAndFetchEntity(mutation, require)
            : GetEntityOrThrow(entityBuilder.Type, entityBuilder.PrimaryKey!.Value, require);
    }

    public SealedEntity UpsertAndFetchEntity(IEntityMutation entityMutation, params IEntityContentRequire[] require)
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
            return EntityConverter.ToSealedEntity(
                entity => _schemaCache.GetEntitySchemaOrThrow(
                    entity.EntityType, entity.SchemaVersion, FetchEntitySchema, GetCatalogSchema
                ),
                grpcResponse.Entity
            );
        });
    }

    public SealedEntity GetEntityOrThrow(string type, int primaryKey, params IEntityContentRequire[] require)
    {
        SealedEntity? entity = GetEntity(type, primaryKey, require);
        return entity ??
               throw new EvitaInvalidUsageException("Entity `" + type + "` with id `" + primaryKey +
                                                    "` doesn't exist!");
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
        if (typeof(SealedEntity).IsAssignableFrom(typeof(T)) &&
            (query.Require == null ||
             FinderVisitor.FindConstraints<IConstraint>(query.Require, x => x is EntityFetch,
                 x => x is ISeparateEntityContentRequireContainer).Count == 0))
            throw new EvitaInvalidUsageException(
                "Method call expects `" + typeof(T).FullName + "` in result, yet it doesn't define `entityFetch` " +
                "in the requirements. This would imply that only entity references " +
                "will be returned by the server!"
            );
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
}