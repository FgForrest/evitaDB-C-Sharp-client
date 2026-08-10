using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class AllowEvolutionModeInCatalogSchemaMutation : ILocalCatalogSchemaMutation
{
    public CatalogEvolutionMode[] EvolutionModes { get; }
    public Operation Operation => Operation.Upsert;

    public AllowEvolutionModeInCatalogSchemaMutation(CatalogEvolutionMode[] evolutionModes)
    {
        EvolutionModes = evolutionModes;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema,
        IEntitySchemaProvider entitySchemaProvider)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        if (catalogSchema!.CatalogEvolutionModes.All(EvolutionModes.Contains))
        {
            // no need to change the schema
            return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(catalogSchema);
        }

        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(
            CatalogSchema.InternalBuild(
                catalogSchema.Version + 1,
                catalogSchema.Name,
                catalogSchema.NameVariants,
                catalogSchema.Description,
                catalogSchema.CatalogEvolutionModes.Concat(EvolutionModes).ToHashSet(),
                catalogSchema.GetAttributes(),
                entitySchemaProvider
            )
        );
    }
}
