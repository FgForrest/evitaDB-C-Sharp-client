using Client.Models.Schemas.Mutations.Catalog;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Catalog;

public class CreateCatalogSchemaMutationConverter : ISchemaMutationConverter<CreateCatalogSchemaMutation, GrpcCreateCatalogSchemaMutation>
{
    public GrpcCreateCatalogSchemaMutation Convert(CreateCatalogSchemaMutation mutation)
    {
        return new GrpcCreateCatalogSchemaMutation{CatalogName = mutation.CatalogName};
    }

    public CreateCatalogSchemaMutation Convert(GrpcCreateCatalogSchemaMutation mutation)
    {
        return new CreateCatalogSchemaMutation(
            mutation.CatalogName
        );
    }
}