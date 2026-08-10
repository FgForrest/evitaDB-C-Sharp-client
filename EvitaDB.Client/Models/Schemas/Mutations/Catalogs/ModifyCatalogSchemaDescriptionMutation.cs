using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class ModifyCatalogSchemaDescriptionMutation : ILocalCatalogSchemaMutation
{
    public string? Description { get; }
    public Operation Operation => Operation.Upsert;

    public ModifyCatalogSchemaDescriptionMutation(string? description)
    {
        Description = description;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema, IEntitySchemaProvider entitySchemaProvider)
    {
        Assert.NotNull(
            catalogSchema,
            () => new InvalidSchemaException("Catalog doesn't exist!")
        );
        if (Equals(Description, catalogSchema!.Description))
        {
            // nothing has changed - we can return existing schema
            return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(catalogSchema);
        }

        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(
            CatalogSchema.InternalBuild(
                catalogSchema.Version + 1,
                catalogSchema.Name,
                catalogSchema.NameVariants,
                Description,
                catalogSchema.CatalogEvolutionModes,
                catalogSchema.GetAttributes(),
                entitySchemaProvider
            )
        );
    }
}
