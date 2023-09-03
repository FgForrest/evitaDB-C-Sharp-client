namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class RemoveEntitySchemaMutation : ILocalCatalogSchemaMutation
{
    public string Name { get; }

    public RemoveEntitySchemaMutation(string name)
    {
        Name = name;
    }
    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        // do nothing - the mutation is handled differently
        return catalogSchema;
    }
}