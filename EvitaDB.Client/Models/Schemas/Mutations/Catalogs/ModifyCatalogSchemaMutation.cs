using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class ModifyCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }
    
    public ILocalCatalogSchemaMutation[] SchemaMutations { get; }
    
    public ModifyCatalogSchemaMutation(string catalogName, params ILocalCatalogSchemaMutation[] schemaMutations)
    {
        CatalogName = catalogName;
        SchemaMutations = schemaMutations;
    }
    
    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema)
    {
        // TODO tpz: fix null problem silenced below by ! operator
        ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? alteredSchema = new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(catalogSchema!);
        ModifyEntitySchemaMutation[]? aggregatedMutations = null;
        foreach (ILocalCatalogSchemaMutation schemaMutation in SchemaMutations) 
        {
            // TODO tpz: fix null problem silenced below by ! operator
            alteredSchema = schemaMutation.Mutate(alteredSchema?.UpdatedCatalogSchema, catalogSchema!);
            if (alteredSchema?.EntitySchemaMutations != null) 
            {
                aggregatedMutations = aggregatedMutations == null ?
                    alteredSchema.EntitySchemaMutations :
                    aggregatedMutations.Concat(alteredSchema.EntitySchemaMutations).ToArray();
            }
        }
        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(alteredSchema?.UpdatedCatalogSchema!, aggregatedMutations);
    }
    
    public Operation Operation => Operation.Upsert;

    public IEnumerable<ChangeCatalogCapture> ToChangeCatalogCapture(MutationPredicate predicate, CaptureContent content)
    {
        MutationPredicateContext context = predicate.Context;
        context.Advance();

        IEnumerable<ChangeCatalogCapture> catalogMutation;
        if (predicate.Test(this))
        {
            catalogMutation = [
                ChangeCatalogCapture.SchemaCapture(
                    context,
                    Operation,
                    content == CaptureContent.Body ? this : null)
            ];
        }
        else
        {
            catalogMutation = [];
        }

        if (context.Direction == IMutation.StreamDirection.Forward)
        {
            return catalogMutation.Concat(SchemaMutations
                .Where(predicate.Test)
                .SelectMany(m => m.ToChangeCatalogCapture(predicate, content)));
        }

        return SchemaMutations
            .OrderByDescending(x => x)
            .Where(predicate.Test)
            .SelectMany(y => y.ToChangeCatalogCapture(predicate, content));
    }
}
