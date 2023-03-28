using Client.Models.Schemas.Mutations.Catalog;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Catalog;

public class RemoveCatalogSchemaMutationConverter : ISchemaMutationConverter<RemoveCatalogSchemaMutation, GrpcRemoveCatalogSchemaMutation>
{
    public GrpcRemoveCatalogSchemaMutation Convert(RemoveCatalogSchemaMutation mutation)
    {
        return new GrpcRemoveCatalogSchemaMutation{CatalogName = mutation.CatalogName};
        
    }

    public RemoveCatalogSchemaMutation Convert(GrpcRemoveCatalogSchemaMutation mutation)
    {
        return new RemoveCatalogSchemaMutation(mutation.CatalogName);
    }
}