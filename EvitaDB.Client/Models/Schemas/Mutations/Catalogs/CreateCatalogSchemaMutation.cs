using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class CreateCatalogSchemaMutation : ITopLevelCatalogSchemaMutation
{
    public string CatalogName { get; }
    
    public CreateCatalogSchemaMutation(string catalogName)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Catalog, catalogName);
        CatalogName = catalogName;
    }
    
    public ICatalogSchema Mutate(ICatalogSchema? catalogSchema) {
        Assert.IsTrue(
            catalogSchema == null,
            () => new InvalidSchemaMutationException("Catalog `" + CatalogName + "` already exists!")
            );
        return CatalogSchema.InternalBuild(
            CatalogName,
            NamingConventionHelper.Generate(CatalogName),
            Enum.GetValues<CatalogEvolutionMode>().ToHashSet(),
            _ => throw new NotSupportedException("Mutated catalog schema can't provide access to entity schemas!"));
    }
}