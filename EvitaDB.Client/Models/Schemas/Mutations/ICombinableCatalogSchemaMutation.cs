using EvitaDB.Client.Models.Schemas.Builders;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ICombinableCatalogSchemaMutation : ILocalCatalogSchemaMutation
{
    MutationCombinationResult<ILocalCatalogSchemaMutation>? CombineWith(
        CatalogSchema currentCatalogSchema, ILocalCatalogSchemaMutation existingMutation
    );
}