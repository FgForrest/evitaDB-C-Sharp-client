namespace Client.Models.Schemas.Mutations.Catalogs;

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

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        return catalogSchema;
    }
}