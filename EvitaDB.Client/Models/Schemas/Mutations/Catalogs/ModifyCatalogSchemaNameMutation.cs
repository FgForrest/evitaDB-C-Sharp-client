using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class ModifyCatalogSchemaNameMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }
    public string NewCatalogName { get; }
    public bool OverwriteTarget { get; }

    public ModifyCatalogSchemaNameMutation(string catalogName, string newCatalogName, bool overwriteTarget)
    {
        CatalogName = catalogName;
        NewCatalogName = newCatalogName;
        OverwriteTarget = overwriteTarget;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.NotNull(catalogSchema, () => new InvalidSchemaException("Catalog doesn't exist!"));
        if (NewCatalogName.Equals(catalogSchema!.Name))
        {
            return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(catalogSchema);
        }

        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(
            CatalogSchema.InternalBuild(
                catalogSchema.Version + 1,
                NewCatalogName,
                NamingConventionHelper.Generate(NewCatalogName),
                catalogSchema.Description,
                catalogSchema.CatalogEvolutionModes,
                catalogSchema.GetAttributes(),
                MutationEntitySchemaAccessor.Instance
            )
        );
    }

    public Operation Operation => Operation.Upsert;

    public IEnumerable<ChangeCatalogCapture> ToChangeCatalogCapture(MutationPredicate predicate, CaptureContent content)
    {
        MutationPredicateContext context = predicate.Context;
        context.Advance();

        if (predicate.Test(this))
        {
            return
            [
                ChangeCatalogCapture.SchemaCapture(
                    context,
                    Operation,
                    content == CaptureContent.Body ? this : null)
            ];
        }

        return [];
    }
}
