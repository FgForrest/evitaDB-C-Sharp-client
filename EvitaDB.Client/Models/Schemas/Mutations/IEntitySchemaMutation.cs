using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface IEntitySchemaMutation : ISchemaMutation
{
    IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema);
}