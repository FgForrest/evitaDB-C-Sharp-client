using Client.DataTypes;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Catalog;

public class CreateEntitySchemaMutation : ILocalCatalogSchemaMutation
{
    public string Name { get; }
    
    public CreateEntitySchemaMutation(string name)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, name);
        Name = name;
    }
    
    public CatalogSchema? Mutate(CatalogSchema? catalogSchema)
    {
        return catalogSchema;
    }
}