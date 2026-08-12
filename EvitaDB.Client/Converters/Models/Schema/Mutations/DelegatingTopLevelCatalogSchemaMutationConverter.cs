using EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations;

/// <summary>
/// Converts top-level (engine) catalog schema mutations to their gRPC representation. Since evitaDB replaced
/// the dedicated `GrpcTopLevelCatalogSchemaMutation` envelope with the broader `GrpcEngineMutation`, this converter
/// targets the engine mutation and covers only its catalog-schema related arms.
/// </summary>
public class DelegatingTopLevelCatalogSchemaMutationConverter : ISchemaMutationConverter<ITopLevelCatalogSchemaMutation, GrpcEngineMutation>
{
    private static readonly DelegatingLocalCatalogSchemaMutationConverter LocalCatalogSchemaMutationConverter = new();

    public GrpcEngineMutation Convert(ITopLevelCatalogSchemaMutation mutation)
    {
        GrpcEngineMutation grpcEngineMutation = new();
        switch (mutation)
        {
            case CreateCatalogSchemaMutation createCatalogSchemaMutation:
                grpcEngineMutation.CreateCatalogSchemaMutation = new CreateCatalogSchemaMutationConverter().Convert(createCatalogSchemaMutation);
                break;
            case ModifyCatalogSchemaNameMutation modifyCatalogSchemaNameMutation:
                grpcEngineMutation.ModifyCatalogSchemaNameMutation = new ModifyCatalogSchemaNameMutationConverter().Convert(modifyCatalogSchemaNameMutation);
                break;
            case ModifyCatalogSchemaMutation modifyCatalogSchemaMutation:
                GrpcModifyCatalogSchemaMutation grpcModifyCatalogSchemaMutation = new()
                {
                    CatalogName = modifyCatalogSchemaMutation.CatalogName
                };
                grpcModifyCatalogSchemaMutation.SchemaMutations.AddRange(
                    modifyCatalogSchemaMutation.SchemaMutations.Select(LocalCatalogSchemaMutationConverter.Convert)
                );
                grpcEngineMutation.ModifyCatalogSchemaMutation = grpcModifyCatalogSchemaMutation;
                break;
            case RemoveCatalogSchemaMutation removeCatalogSchemaMutation:
                grpcEngineMutation.RemoveCatalogSchemaMutation = new RemoveCatalogSchemaMutationConverter().Convert(removeCatalogSchemaMutation);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation), mutation.GetType().Name,
                    "Unsupported top-level catalog schema mutation type.");
        }
        return grpcEngineMutation;
    }

    public ITopLevelCatalogSchemaMutation Convert(GrpcEngineMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcEngineMutation.MutationOneofCase.CreateCatalogSchemaMutation =>
                new CreateCatalogSchemaMutationConverter().Convert(mutation.CreateCatalogSchemaMutation),
            GrpcEngineMutation.MutationOneofCase.ModifyCatalogSchemaNameMutation =>
                new ModifyCatalogSchemaNameMutationConverter().Convert(mutation.ModifyCatalogSchemaNameMutation),
            GrpcEngineMutation.MutationOneofCase.ModifyCatalogSchemaMutation =>
                new ModifyCatalogSchemaMutation(
                    mutation.ModifyCatalogSchemaMutation.CatalogName,
                    mutation.ModifyCatalogSchemaMutation.SchemaMutations
                        .Select(LocalCatalogSchemaMutationConverter.Convert)
                        .ToArray()
                ),
            GrpcEngineMutation.MutationOneofCase.RemoveCatalogSchemaMutation =>
                new RemoveCatalogSchemaMutationConverter().Convert(mutation.RemoveCatalogSchemaMutation),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation.MutationCase,
                "Unsupported engine mutation type.")
        };
    }
}
