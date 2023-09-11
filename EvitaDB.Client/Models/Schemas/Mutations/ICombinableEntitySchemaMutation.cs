using EvitaDB.Client.Models.Schemas.Builders;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ICombinableEntitySchemaMutation : IEntitySchemaMutation
{
    MutationCombinationResult<IEntitySchemaMutation>? CombineWith(
        ICatalogSchema currentCatalogSchema,
    IEntitySchema currentEntitySchema,
    IEntitySchemaMutation existingMutation
    );
}