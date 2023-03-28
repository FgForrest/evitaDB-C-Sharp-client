using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations.Catalog;

public class ModifyCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }
    
    public ILocalCatalogSchemaMutation[] SchemaMutations { get; }
    
    public ModifyCatalogSchemaMutation(string catalogName, params ILocalCatalogSchemaMutation[] schemaMutations)
    {
        CatalogName = catalogName;
        SchemaMutations = schemaMutations;
    }
    
    public CatalogSchema? Mutate(CatalogSchema? catalogSchema)
    {
        CatalogSchema? alteredSchema = catalogSchema;
        foreach (ILocalCatalogSchemaMutation schemaMutation in SchemaMutations) {
            alteredSchema = schemaMutation.Mutate(alteredSchema);
        }
        return alteredSchema;
    }
}