using EvitaDB.Client.Models.Schemas.Builders;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ICombinableEntitySchemaMutation : IEntitySchemaMutation
{
    MutationCombinationResult<IEntitySchemaMutation>? CombineWith(
        CatalogSchema currentCatalogSchema,
    EntitySchema currentEntitySchema,
    IEntitySchemaMutation existingMutation
    );
}