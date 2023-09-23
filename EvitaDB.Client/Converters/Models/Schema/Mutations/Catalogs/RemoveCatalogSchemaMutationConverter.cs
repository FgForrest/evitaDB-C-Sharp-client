using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

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