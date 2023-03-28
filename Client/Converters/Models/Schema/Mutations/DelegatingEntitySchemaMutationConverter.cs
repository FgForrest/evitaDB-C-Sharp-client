using Client.Converters.Models.Schema.Mutations.Attributes;
using Client.Models.Schemas.Mutations;
using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations;

public class DelegatingEntitySchemaMutationConverter : ISchemaMutationConverter<IEntitySchemaMutation, GrpcEntitySchemaMutation>
{
    public GrpcEntitySchemaMutation Convert(IEntitySchemaMutation mutation)
    {
        GrpcEntitySchemaMutation grpcTopLevelCatalogSchemaMutation = new();
        switch (mutation)
        {
            case CreateAttributeSchemaMutation createEntitySchemaMutation: 
                grpcTopLevelCatalogSchemaMutation.CreateAttributeSchemaMutation = new CreateAttributeSchemaMutationConverter().Convert(createEntitySchemaMutation);
                break;
            default:
                throw new NotImplementedException();
        }
        return grpcTopLevelCatalogSchemaMutation;
    }

    public IEntitySchemaMutation Convert(GrpcEntitySchemaMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcEntitySchemaMutation.MutationOneofCase.CreateAttributeSchemaMutation => new CreateAttributeSchemaMutationConverter().Convert(mutation.CreateAttributeSchemaMutation),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}