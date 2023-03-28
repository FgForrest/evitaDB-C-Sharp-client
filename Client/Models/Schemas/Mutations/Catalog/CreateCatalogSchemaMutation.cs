using Client.DataTypes;
using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Catalog;

public class CreateCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }
    
    public CreateCatalogSchemaMutation(string catalogName)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Catalog, catalogName);
        CatalogName = catalogName;
    }
    
    public CatalogSchema Mutate(CatalogSchema? catalogSchema) {
        Assert.IsTrue(
            catalogSchema == null,
            () => new InvalidSchemaMutationException("Catalog `" + CatalogName + "` already exists!")
            );
        return CatalogSchema.InternalBuild(
            CatalogName,
            NamingConventionHelper.Generate(CatalogName),
            _ => throw new NotSupportedException("Mutated catalog schema can't provide access to entity schemas!"));
    }
}