using Client.Exceptions;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Catalogs;

public class RemoveCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }

    public RemoveCatalogSchemaMutation(string catalogName)
    {
        CatalogName = catalogName;
    }
    
    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.NotNull(
            catalogSchema,
            () => new InvalidSchemaMutationException("Catalog `" + CatalogName + "` doesn't exist!")
        );
        return null;
    }
}