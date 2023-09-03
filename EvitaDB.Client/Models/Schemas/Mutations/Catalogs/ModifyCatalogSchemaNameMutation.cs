using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class ModifyCatalogSchemaNameMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }
    public string NewCatalogName { get;  }
    public bool OverwriteTarget { get; }

    public ModifyCatalogSchemaNameMutation(string catalogName, string newCatalogName, bool overwriteTarget)
    {
        CatalogName = catalogName;
        NewCatalogName = newCatalogName;
        OverwriteTarget = overwriteTarget;
    }

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.NotNull(catalogSchema, () => new InvalidSchemaMutationException("Catalog doesn't exist!"));
        if (NewCatalogName.Equals(catalogSchema!.Name)) {
            return catalogSchema;
        }
        return CatalogSchema.InternalBuild(
            catalogSchema.Version + 1,
            NewCatalogName,
            NamingConventionHelper.Generate(NewCatalogName),
            catalogSchema.Description,
            catalogSchema.CatalogEvolutionModes,
            catalogSchema.GetAttributes(),
            entityType => throw new NotSupportedException("Mutated catalog schema can't provide access to entity schemas!"));
    }
}