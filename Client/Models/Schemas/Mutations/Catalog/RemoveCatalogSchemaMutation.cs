using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Catalog;

public class RemoveCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }

    public RemoveCatalogSchemaMutation(string catalogName)
    {
        CatalogName = catalogName;
    }
    
    public CatalogSchema? Mutate(CatalogSchema? catalogSchema)
    {
        Assert.NotNull(
            catalogSchema,
            () => new InvalidSchemaMutationException("Catalog `" + CatalogName + "` doesn't exist!")
        );
        return null;
    }
}