using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class RemoveCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }

    public RemoveCatalogSchemaMutation(string catalogName)
    {
        CatalogName = catalogName;
    }
    
    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.NotNull(
            catalogSchema,
            () => new InvalidSchemaException("Catalog `" + CatalogName + "` doesn't exist!")
        );
        return null;
    }

    public Operation Operation => Operation.Remove;
    public IEnumerable<ChangeCatalogCapture> ToChangeCatalogCapture(MutationPredicate predicate, CaptureContent content)
    {
        if (predicate.Test(this))
        {
            MutationPredicateContext context = predicate.Context;
            context.Advance();
            return [
                ChangeCatalogCapture.SchemaCapture(
                    context,
                    Operation,
                    content == CaptureContent.Body ? this : null)
            ];
        }

        return [];
    }
}
