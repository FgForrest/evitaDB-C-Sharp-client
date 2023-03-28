using Client.Converters.Models.Schema.Mutations.Catalog;
using Client.Models.Schemas.Mutations;
using Client.Models.Schemas.Mutations.Catalog;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations;

public class DelegatingLocalCatalogSchemaMutationConverter : ISchemaMutationConverter<ILocalCatalogSchemaMutation, GrpcLocalCatalogSchemaMutation>
{
    
    public GrpcLocalCatalogSchemaMutation Convert(ILocalCatalogSchemaMutation mutation)
    {
        GrpcLocalCatalogSchemaMutation grpcTopLevelCatalogSchemaMutation = new();
        switch (mutation)
        {
            case CreateEntitySchemaMutation createEntitySchemaMutation: 
                grpcTopLevelCatalogSchemaMutation.CreateEntitySchemaMutation = new CreateEntitySchemaMutationConverter().Convert(createEntitySchemaMutation);
                break;
            case ModifyEntitySchemaMutation modifyEntitySchemaMutation: 
                grpcTopLevelCatalogSchemaMutation.ModifyEntitySchemaMutation = new ModifyEntitySchemaMutationConverter().Convert(modifyEntitySchemaMutation);
                break;
            default:
                throw new NotImplementedException();
        }
        return grpcTopLevelCatalogSchemaMutation;
    }

    public ILocalCatalogSchemaMutation Convert(GrpcLocalCatalogSchemaMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.CreateEntitySchemaMutation => new CreateEntitySchemaMutationConverter().Convert(mutation.CreateEntitySchemaMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyEntitySchemaMutation => new ModifyEntitySchemaMutationConverter().Convert(mutation.ModifyEntitySchemaMutation),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}