using Client.DataTypes;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Catalogs;

public class CreateEntitySchemaMutation : ILocalCatalogSchemaMutation
{
    public string Name { get; }
    
    public CreateEntitySchemaMutation(string name)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, name);
        Name = name;
    }
    
    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        return catalogSchema;
    }
}