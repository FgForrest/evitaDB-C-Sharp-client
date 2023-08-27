using Client.Models.Schemas.Builders;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations;

public interface ICombinableEntitySchemaMutation : IEntitySchemaMutation
{
    MutationCombinationResult<IEntitySchemaMutation>? CombineWith(
        CatalogSchema currentCatalogSchema,
    EntitySchema currentEntitySchema,
    IEntitySchemaMutation existingMutation
    );
}