using Client.Models.Schemas.Builders;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations;

public interface ICombinableCatalogSchemaMutation : ILocalCatalogSchemaMutation
{
    MutationCombinationResult<ILocalCatalogSchemaMutation>? CombineWith(
        CatalogSchema currentCatalogSchema, ILocalCatalogSchemaMutation existingMutation
    );
}