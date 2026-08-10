using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class CreateEntitySchemaMutation : ILocalCatalogSchemaMutation, ICatalogSchemaMutation
{
    public string Name { get; }
    
    public CreateEntitySchemaMutation(string name)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, name);
        Name = name;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema,
        IEntitySchemaProvider entitySchemaProvider)
    {
        if (entitySchemaProvider is MutationEntitySchemaAccessor mutationEntitySchemaAccessor) 
        {
            mutationEntitySchemaAccessor.AddUpsertedEntitySchema(EntitySchema.InternalBuild(Name));
        }
        // TODO tpz: solve nullability issue below
        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(catalogSchema!);
    }

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        return catalogSchema;
    }

    public Operation Operation => Operation.Upsert;

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
