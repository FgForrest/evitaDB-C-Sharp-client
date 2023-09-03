namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ICatalogSchemaMutation : ISchemaMutation
{
    ICatalogSchema? Mutate(ICatalogSchema? catalogSchema);
}