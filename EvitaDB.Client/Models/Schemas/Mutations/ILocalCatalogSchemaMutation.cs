using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ILocalCatalogSchemaMutation : ICatalogSchemaMutation
{
    CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema, IEntitySchemaProvider entitySchemaProvider); 
    
    CatalogSchemaWithImpactOnEntitySchemas? ICatalogSchemaMutation.Mutate(ICatalogSchema? catalogSchema)
    {
        return Mutate(catalogSchema, MutationEntitySchemaAccessor.Instance);
    }
}
