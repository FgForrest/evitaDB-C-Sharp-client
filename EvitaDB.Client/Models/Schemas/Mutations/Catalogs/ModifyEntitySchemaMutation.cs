using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class ModifyEntitySchemaMutation : ILocalCatalogSchemaMutation, IEntitySchemaMutation
{
    public string EntityType { get; }
    public IEntitySchemaMutation[] SchemaMutations { get; }
    
    public ModifyEntitySchemaMutation(string entityType, params IEntitySchemaMutation[] schemaMutations)
    {
        EntityType = entityType;
        SchemaMutations = schemaMutations;
    }
    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        IEntitySchema? alteredSchema = entitySchema;
        foreach (IEntitySchemaMutation schemaMutation in SchemaMutations) {
            alteredSchema = schemaMutation.Mutate(catalogSchema, alteredSchema);
        }
        return alteredSchema;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema, IEntitySchemaProvider entitySchemaProvider)
    {
        if (entitySchemaProvider is MutationEntitySchemaAccessor mutationEntitySchemaAccessor)
        {
            var entitySchema = mutationEntitySchemaAccessor.GetEntitySchema(EntityType);
            // TODO tpz: solve nullability issue below
            IEntitySchema? alteredSchema = Mutate(catalogSchema!, entitySchema);
            if (alteredSchema is not null)
            {
                mutationEntitySchemaAccessor.AddUpsertedEntitySchema(alteredSchema);
            }
            else
            {
                throw new EvitaInternalError("Entity schema not found: " + EntityType);
            }
            
        }
        // do nothing - we alter only the entity schema
        // TODO tpz: solve nullability issue below
        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(
            catalogSchema!
        );
    }

    public Operation Operation => Operation.Upsert;
    public IEnumerable<ChangeCatalogCapture> ToChangeCatalogCapture(MutationPredicate predicate, CaptureContent content)
    {
        MutationPredicateContext context = predicate.Context;
        context.Advance();
        context.SetEntityType(EntityType);
        
        IEnumerable<ChangeCatalogCapture> entitySchemaCapture;
        if (predicate.Test(this))
        {
            entitySchemaCapture = [
                ChangeCatalogCapture.SchemaCapture(
                    context,
                    Operation,
                    content == CaptureContent.Body ? this : null)
            ];
        }
        else
        {
            entitySchemaCapture = [];
        }

        if (context.Direction == IMutation.StreamDirection.Forward)
        {
            return entitySchemaCapture.Concat(
                SchemaMutations
                    .Where(predicate.Test)
                    .SelectMany(m => m.ToChangeCatalogCapture(predicate, content))
            );
        }

        return SchemaMutations
            .OrderByDescending(x => x)
            .SelectMany(y => y.ToChangeCatalogCapture(predicate, content))
            .Concat(entitySchemaCapture);
    }
}
