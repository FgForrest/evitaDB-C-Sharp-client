using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Catalogs;

public class DisallowEvolutionModeInCatalogSchemaMutation : ILocalCatalogSchemaMutation
{
    public ISet<CatalogEvolutionMode> EvolutionModes { get; }

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


    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        if (!catalogSchema!.CatalogEvolutionModes.Any(EvolutionModes.Contains)) {
            // no need to change the schema
            return catalogSchema;
        }

        return CatalogSchema.InternalBuild(
            catalogSchema.Version + 1,
            catalogSchema.Name,
            catalogSchema.NameVariants,
            catalogSchema.Description,
            catalogSchema.CatalogEvolutionModes
                .Where(it => !EvolutionModes.Contains(it))
                .ToHashSet(),
            catalogSchema.GetAttributes(),
            _ => throw new NotSupportedException("Mutated catalog schema can't provide access to entity schemas!"));
    }
}