using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class RemoveEntitySchemaMutation : ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public Operation Operation => Operation.Remove;

    public RemoveEntitySchemaMutation(string name)
    {
        Name = name;
    }
    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema, IEntitySchemaProvider entitySchemaProvider)
    {
        if (entitySchemaProvider is MutationEntitySchemaAccessor mutationEntitySchemaAccessor)
        {
            IEntitySchema? entitySchema = mutationEntitySchemaAccessor.GetEntitySchema(Name);
            if (entitySchema is not null)
            {
                mutationEntitySchemaAccessor.RemoveEntitySchema(Name);
            }
            else
            {
                throw new EvitaInternalError("Entity schema not found: " + Name);
            }
            
        }
        // do nothing - we alter only the entity schema
        // TODO tpz: solve nullability issue below
        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(
            catalogSchema!
        );
    }
}
