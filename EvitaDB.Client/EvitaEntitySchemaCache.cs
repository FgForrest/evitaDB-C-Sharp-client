using System.Collections.Concurrent;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client;

public class EvitaEntitySchemaCache
{
    public string CatalogName { get; }
    private ConcurrentDictionary<ISchemaCacheKey, SchemaWrapper> CachedSchemas { get; } = new();
    private long _lastObsoleteCheck;

    public EvitaEntitySchemaCache(string catalogName)
    {
        CatalogName = catalogName;
    }

    public void AnalyzeMutations(params ISchemaMutation[] schemaMutations)
    {
        foreach (ISchemaMutation mutation in schemaMutations) {
            if (mutation is ModifyEntitySchemaMutation modifyEntitySchemaMutation) {
                RemoveLatestEntitySchema(modifyEntitySchemaMutation.EntityType);
            } else if (mutation is ModifyCatalogSchemaMutation entityRelatedMutation) {
                AnalyzeMutations(entityRelatedMutation.SchemaMutations);
            } else if (mutation is ICatalogSchemaMutation) {
                RemoveLatestCatalogSchema();
            }
        }
    }

    public CatalogSchema GetLatestCatalogSchema(Func<CatalogSchema> schemaAccessor)
    {
        long now = CurrentTimeMillis();
        // each minute apply obsolete check
        long lastCheck = Interlocked.Read(ref _lastObsoleteCheck);
        if (now < lastCheck + 60 * 1000)
        {
            Interlocked.CompareExchange(ref _lastObsoleteCheck, lastCheck, now);
            if (Interlocked.Read(ref _lastObsoleteCheck) != lastCheck)
            {
                List<SchemaWrapper> toRemove = new();
                foreach (KeyValuePair<ISchemaCacheKey, SchemaWrapper> keyValuePair in CachedSchemas)
                {
                    if (keyValuePair.Value.Obsolete(now))
                    {
                        toRemove.Add(keyValuePair.Value);
                    }
                }

                toRemove.ForEach(x => CachedSchemas.Values.Remove(x));
            }
        }

        // attempt to retrieve schema from the client side cache
        CachedSchemas.TryGetValue(LatestCatalogSchema.Instance, out SchemaWrapper? schemaWrapper);
        if (schemaWrapper == null)
        {
            // if not found or versions don't match - re-fetch the contents
            CatalogSchema schemaRelevantToSession = schemaAccessor.Invoke();
            SchemaWrapper newCachedValue = new SchemaWrapper(schemaRelevantToSession, now);
            CachedSchemas.GetOrAdd(
                LatestCatalogSchema.Instance,
                newCachedValue
            );
            return schemaRelevantToSession;
        }

        // if found in cache, update last used timestamp
        schemaWrapper.Used();
        return schemaWrapper.CatalogSchema!;
    }

    public void SetLatestCatalogSchema(CatalogSchema catalogSchema)
    {
        CachedSchemas[LatestCatalogSchema.Instance] = new SchemaWrapper(catalogSchema, CurrentTimeMillis());
    }

    public void RemoveLatestCatalogSchema()
    {
        CachedSchemas.Remove(LatestCatalogSchema.Instance, out _);
    }

    public EntitySchema? GetEntitySchema(string entityType, int version, Func<string, EntitySchema?> schemaAccessor)
    {
        return FetchEntitySchema(
            new EntitySchemaWithVersion(entityType, version),
            schemaWrapper => schemaWrapper is null || schemaWrapper.EntitySchema?.Version != version,
            schemaAccessor
        );
    }

    public EntitySchema GetEntitySchemaOrThrow(string entityType, int version,
        Func<string, EntitySchema?> schemaAccessor,
        Func<CatalogSchema> catalogSchemaSupplier
    )
    {
        return GetEntitySchema(entityType, version, schemaAccessor) ??
               throw new CollectionNotFoundException($"Schema for entity type {entityType} not found");
        //.map(it -> new EntitySchemaDecorator(catalogSchemaSupplier, it))
        //.orElseThrow(() -> new CollectionNotFoundException(entityType));
    }

    public EntitySchema? GetLatestEntitySchema(string entityType, Func<string, EntitySchema?> schemaAccessor,
        Action<CatalogSchema> catalogSchemaSupplier
    )
    {
        //TODO OPEN FOR WRITE
        return FetchEntitySchema(
            new LatestEntitySchema(entityType),
            schemaWrapper => schemaWrapper is null,
            schemaAccessor
        );
    }

    public void SetLatestEntitySchema(EntitySchema entitySchema)
    {
        CachedSchemas.GetOrAdd(
            new LatestEntitySchema(entitySchema.Name),
            new SchemaWrapper(entitySchema, CurrentTimeMillis())
        );
    }

    /**
	 * Method resets tha last known {@link EntitySchemaContract} to NULL. This will force to fetch actual schema from
	 * the server side next time, it's asked for it.
	 */
    public void RemoveLatestEntitySchema(string entityType)
    {
        CachedSchemas.Remove(new LatestEntitySchema(entityType), out _);
    }

    private EntitySchema? FetchEntitySchema(
        IEntitySchemaCacheKey cacheKey,
        Predicate<SchemaWrapper?> shouldReFetch,
        Func<string, EntitySchema?> schemaAccessor
    )
    {
        long now = CurrentTimeMillis();
        // each minute apply obsolete check
        long lastCheck = Interlocked.Read(ref _lastObsoleteCheck);
        if (now < lastCheck + 60 * 1000)
        {
            Interlocked.CompareExchange(ref _lastObsoleteCheck, lastCheck, now);
            if (Interlocked.Read(ref _lastObsoleteCheck) != lastCheck)
            {
                List<SchemaWrapper> toRemove = new();
                foreach (KeyValuePair<ISchemaCacheKey, SchemaWrapper> keyValuePair in CachedSchemas)
                {
                    if (keyValuePair.Value.Obsolete(now))
                    {
                        toRemove.Add(keyValuePair.Value);
                    }
                }

                toRemove.ForEach(x => CachedSchemas.Values.Remove(x));
            }
        }

        // attempt to retrieve schema from the client side cache
        CachedSchemas.TryGetValue(cacheKey, out SchemaWrapper? schemaWrapper);
        if (shouldReFetch.Invoke(schemaWrapper))
        {
            // if not found or versions don't match - re-fetch the contents
            EntitySchema? schemaRelevantToSession = schemaAccessor.Invoke(cacheKey.EntityType);
            if (schemaRelevantToSession is not null)
            {
                SchemaWrapper newCachedValue = new SchemaWrapper(schemaRelevantToSession, now);
                CachedSchemas.GetOrAdd(
                    new EntitySchemaWithVersion(cacheKey.EntityType, schemaRelevantToSession.Version),
                    newCachedValue
                );
                // initialize the latest known entity schema if missing
                LatestEntitySchema latestEntitySchema = new LatestEntitySchema(cacheKey.EntityType);
                SchemaWrapper? latestCachedVersion =
                    CachedSchemas.TryGetValue(latestEntitySchema, out SchemaWrapper? latest)
                        ? latest
                        : null;
                CachedSchemas.GetOrAdd(latestEntitySchema, newCachedValue);
                // if not missing verify the stored value is really the latest one and if not rewrite it
                if (latestCachedVersion != null &&
                    latestCachedVersion.EntitySchema?.Version < newCachedValue.EntitySchema?.Version)
                {
                    CachedSchemas.GetOrAdd(latestEntitySchema, newCachedValue);
                }
            }

            return schemaRelevantToSession;
        }

        // if found in cache, update last used timestamp
        schemaWrapper?.Used();
        return schemaWrapper?.EntitySchema;
    }

    private interface ISchemaCacheKey
    {
    }

    private interface IEntitySchemaCacheKey : ISchemaCacheKey
    {
        public string EntityType { get; }
    }

    private record EntitySchemaWithVersion(string EntityType, int Version) : IEntitySchemaCacheKey;

    private record LatestEntitySchema(string EntityType) : IEntitySchemaCacheKey;

    private record LatestCatalogSchema : ISchemaCacheKey
    {
        public static readonly LatestCatalogSchema Instance = new();
    }

    private class SchemaWrapper
    {
        internal static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /**
		 * The entity schema is considered obsolete after 4 hours since last usage.
		 */
        private const long ObsoleteInterval = 4L * 60L * 60L * 100L;

        /**
		 * The entity schema fetched from the server.
		 */
        public CatalogSchema? CatalogSchema { get; }

        /**
		 * The entity schema fetched from the server.
		 */
        public EntitySchema? EntitySchema { get; }

        /**
		 * Date and time ({@link System#currentTimeMillis()} of the moment when the entity schema was fetched from
		 * the server side.
		 */
        public long Fetched { get; }

        /**
		 * Date and time ({@link System#currentTimeMillis()} of the moment when the entity schema was used for
		 * the last tim.
		 */
        private long LastUsed { get; set; }

        public SchemaWrapper(CatalogSchema catalogSchema, long fetched)
        {
            CatalogSchema = catalogSchema;
            EntitySchema = null;
            Fetched = fetched;
            LastUsed = fetched;
        }

        public SchemaWrapper(EntitySchema entitySchema, long fetched)
        {
            CatalogSchema = null;
            EntitySchema = entitySchema;
            Fetched = fetched;
            LastUsed = fetched;
        }

        /**
		 * Tracks the moment when the entity schema was used for the last time.
		 */
        public void Used()
        {
            LastUsed = CurrentTimeMillis();
        }

        /**
		 * Returns TRUE if the entity schema was used long ago (defined by the {@link #OBSOLETE_INTERVAL}.
		 */
        public bool Obsolete(long now)
        {
            return now - ObsoleteInterval > LastUsed;
        }
    }

    private static long CurrentTimeMillis()
    {
        return (long) (DateTime.UtcNow - SchemaWrapper.Epoch).TotalMilliseconds;
    }
}