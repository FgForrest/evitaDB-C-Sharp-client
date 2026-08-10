using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ICatalogSchemaMutation : ISchemaMutation
{
    CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema);

    record CatalogSchemaWithImpactOnEntitySchemas(
        ICatalogSchema UpdatedCatalogSchema,
        ModifyEntitySchemaMutation[]? EntitySchemaMutations
    )
    {
        public CatalogSchemaWithImpactOnEntitySchemas(ICatalogSchema updatedCatalogSchema) : this(updatedCatalogSchema, null)
        {
        }
    };
}
