using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class CreateCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }

    public CreateCatalogSchemaMutation(string catalogName)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Catalog, catalogName);
        CatalogName = catalogName;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsTrue(
            catalogSchema == null,
            () => new InvalidSchemaException("Catalog `" + CatalogName + "` already exists!")
        );
        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(
            CatalogSchema.InternalBuild(
                CatalogName,
                NamingConventionHelper.Generate(CatalogName),
                Enum.GetValues<CatalogEvolutionMode>().ToHashSet(),
                MutationEntitySchemaAccessor.Instance
            )
        );
    }

    public Operation Operation => Operation.Upsert;

    public IEnumerable<ChangeCatalogCapture> ToChangeCatalogCapture(MutationPredicate predicate, CaptureContent content)
    {
        if (predicate.Test(this))
        {
            MutationPredicateContext context = predicate.Context;
            context.Advance();
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
