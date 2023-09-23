using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

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