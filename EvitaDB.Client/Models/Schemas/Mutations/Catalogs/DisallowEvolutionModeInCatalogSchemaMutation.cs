using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class DisallowEvolutionModeInCatalogSchemaMutation : ILocalCatalogSchemaMutation
{
    public ISet<CatalogEvolutionMode> EvolutionModes { get; }
    public Operation Operation => Operation.Upsert;

    public DisallowEvolutionModeInCatalogSchemaMutation(ISet<CatalogEvolutionMode> evolutionModes)
    {
        EvolutionModes = new HashSet<CatalogEvolutionMode>();
        foreach (var evolutionMode in evolutionModes)
        {
            EvolutionModes.Add(evolutionMode);
        }
    }

    public DisallowEvolutionModeInCatalogSchemaMutation(params CatalogEvolutionMode[] evolutionModes)
    {
        EvolutionModes = new HashSet<CatalogEvolutionMode>();
        foreach (var evolutionMode in evolutionModes)
        {
            EvolutionModes.Add(evolutionMode);
        }
    }


    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema,
        IEntitySchemaProvider entitySchemaProvider)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        if (!catalogSchema!.CatalogEvolutionModes.Any(EvolutionModes.Contains))
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
                catalogSchema.CatalogEvolutionModes
                    .Where(it => !EvolutionModes.Contains(it))
                    .ToHashSet(),
                catalogSchema.GetAttributes(),
                entitySchemaProvider
            )
        );
    }
}
