using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations.Conflicts;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

/// <summary>
/// Mutation that changes the conflict resolution behaviour of the entire catalog. Note: the local schema model does not represent conflict-resolution settings yet, so the local
/// application is a documented no-op - the server applies the mutation authoritatively.
/// </summary>
public class ModifyCatalogSchemaConflictResolutionMutation : ILocalCatalogSchemaMutation
{
    public ConflictResolution? ConflictResolution { get; }

    public Operation Operation => Operation.Upsert;

    public ModifyCatalogSchemaConflictResolutionMutation(ConflictResolution? conflictResolution)
    {
        ConflictResolution = conflictResolution;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema,
        IEntitySchemaProvider entitySchemaProvider)
    {
        return catalogSchema is null
            ? null
            : new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(catalogSchema);
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema)
    {
        return Mutate(catalogSchema, MutationEntitySchemaAccessor.Instance);
    }
}
