using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Catalogs;

public class AllowEvolutionModeInCatalogSchemaMutation : ILocalCatalogSchemaMutation
{
    public CatalogEvolutionMode[] EvolutionModes { get; }

    public AllowEvolutionModeInCatalogSchemaMutation(CatalogEvolutionMode[] evolutionModes)
    {
        EvolutionModes = evolutionModes;
    }
    
    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        if (catalogSchema!.CatalogEvolutionModes.All(EvolutionModes.Contains)) {
            // no need to change the schema
            return catalogSchema;
        } else {
            return CatalogSchema.InternalBuild(
                catalogSchema.Version + 1,
                catalogSchema.Name,
                catalogSchema.NameVariants,
                catalogSchema.Description,
                catalogSchema.CatalogEvolutionModes.Concat(EvolutionModes).ToHashSet(),
                catalogSchema.GetAttributes(),
                _ => throw new NotSupportedException("Mutated catalog schema can't provide access to entity schemas!"));
        }
    }
}