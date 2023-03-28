using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations;

public interface IEntitySchemaMutation : ISchemaMutation
{
    EntitySchema? Mutate(CatalogSchema catalogSchema, EntitySchema? entitySchema);
}