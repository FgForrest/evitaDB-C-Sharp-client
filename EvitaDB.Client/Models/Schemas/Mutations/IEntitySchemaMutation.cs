using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations;

public interface IEntitySchemaMutation : ISchemaMutation
{
    IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema);
}