using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations.Catalog;

public class ModifyEntitySchemaMutation : ILocalCatalogSchemaMutation, IEntitySchemaMutation
{
    public string EntityType { get; }
    public IEntitySchemaMutation[] SchemaMutations { get; }
    
    public ModifyEntitySchemaMutation(string entityType, params IEntitySchemaMutation[] schemaMutations)
    {
        EntityType = entityType;
        SchemaMutations = schemaMutations;
    }
    public EntitySchema? Mutate(CatalogSchema catalogSchema, EntitySchema? entitySchema)
    {
        EntitySchema? alteredSchema = entitySchema;
        foreach (IEntitySchemaMutation schemaMutation in SchemaMutations) {
            alteredSchema = schemaMutation.Mutate(catalogSchema, alteredSchema);
        }
        return alteredSchema;
    }

    public CatalogSchema? Mutate(CatalogSchema? catalogSchema)
    {
        return catalogSchema;
    }
}