namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ITopLevelCatalogSchemaMutation : ICatalogSchemaMutation
{
    public string CatalogName { get; }
}