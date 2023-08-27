using Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Catalogs;

public class AllowEvolutionModeInCatalogSchemaMutationConverter : ISchemaMutationConverter<
    AllowEvolutionModeInCatalogSchemaMutation, GrpcAllowEvolutionModeInCatalogSchemaMutation>
{
    public GrpcAllowEvolutionModeInCatalogSchemaMutation Convert(AllowEvolutionModeInCatalogSchemaMutation mutation)
    {
        return new GrpcAllowEvolutionModeInCatalogSchemaMutation
        {
            EvolutionModes = {mutation.EvolutionModes.Select(EvitaEnumConverter.ToGrpcCatalogEvolutionMode)}
        };
    }

    public AllowEvolutionModeInCatalogSchemaMutation Convert(GrpcAllowEvolutionModeInCatalogSchemaMutation mutation)
    {
        return new AllowEvolutionModeInCatalogSchemaMutation(mutation.EvolutionModes
            .Select(EvitaEnumConverter.ToCatalogEvolutionMode).ToArray());
    }
}