namespace EvitaDB.Client.Models.Schemas.Dtos;

public interface IEntitySchemaProvider
{
    IEnumerable<IEntitySchema?> GetEntitySchemas();
    IEntitySchema? GetEntitySchema(string name);
}
