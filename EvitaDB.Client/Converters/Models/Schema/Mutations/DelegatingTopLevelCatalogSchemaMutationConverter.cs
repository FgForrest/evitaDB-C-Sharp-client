using EvitaDB;
using EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations;

public class DelegatingTopLevelCatalogSchemaMutationConverter : ISchemaMutationConverter<ITopLevelCatalogSchemaMutation, GrpcTopLevelCatalogSchemaMutation>
{
    public GrpcTopLevelCatalogSchemaMutation Convert(ITopLevelCatalogSchemaMutation mutation)
    {
        GrpcTopLevelCatalogSchemaMutation grpcTopLevelCatalogSchemaMutation = new();
        switch (mutation)
        {
            case CreateCatalogSchemaMutation createCatalogSchemaMutation: 
                grpcTopLevelCatalogSchemaMutation.CreateCatalogSchemaMutation = new CreateCatalogSchemaMutationConverter().Convert(createCatalogSchemaMutation);
                break;
            case ModifyCatalogSchemaNameMutation modifyCatalogSchemaMutation: 
                grpcTopLevelCatalogSchemaMutation.ModifyCatalogSchemaNameMutation = new ModifyCatalogSchemaNameMutationConverter().Convert(modifyCatalogSchemaMutation);
                break;
            case RemoveCatalogSchemaMutation removeCatalogSchemaMutation: 
                grpcTopLevelCatalogSchemaMutation.RemoveCatalogSchemaMutation = new RemoveCatalogSchemaMutationConverter().Convert(removeCatalogSchemaMutation);
                break;
        }
        return grpcTopLevelCatalogSchemaMutation;
    }

    public ITopLevelCatalogSchemaMutation Convert(GrpcTopLevelCatalogSchemaMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcTopLevelCatalogSchemaMutation.MutationOneofCase.CreateCatalogSchemaMutation => new CreateCatalogSchemaMutationConverter().Convert(mutation.CreateCatalogSchemaMutation),  
            GrpcTopLevelCatalogSchemaMutation.MutationOneofCase.ModifyCatalogSchemaNameMutation => new ModifyCatalogSchemaNameMutationConverter().Convert(mutation.ModifyCatalogSchemaNameMutation),  
            GrpcTopLevelCatalogSchemaMutation.MutationOneofCase.RemoveCatalogSchemaMutation => new RemoveCatalogSchemaMutationConverter().Convert(mutation.RemoveCatalogSchemaMutation),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}