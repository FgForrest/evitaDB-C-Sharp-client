namespace Client.Models.Schemas.Mutations.Catalogs;

public class ModifyCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }
    
    public ILocalCatalogSchemaMutation[] SchemaMutations { get; }
    
    public ModifyCatalogSchemaMutation(string catalogName, params ILocalCatalogSchemaMutation[] schemaMutations)
    {
        CatalogName = catalogName;
        SchemaMutations = schemaMutations;
    }
    
    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        ICatalogSchema? alteredSchema = catalogSchema;
        foreach (ILocalCatalogSchemaMutation schemaMutation in SchemaMutations) {
            alteredSchema = schemaMutation.Mutate(alteredSchema);
        }
        return alteredSchema;
    }
}