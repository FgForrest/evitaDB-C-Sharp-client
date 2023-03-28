using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations;

public interface ICatalogSchemaMutation : ISchemaMutation
{
    CatalogSchema? Mutate(CatalogSchema? catalogSchema);
}