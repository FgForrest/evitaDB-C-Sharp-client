using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

public class DisallowEvolutionModeInCatalogSchemaMutationConverter : ISchemaMutationConverter<
    DisallowEvolutionModeInCatalogSchemaMutation, GrpcDisallowEvolutionModeInCatalogSchemaMutation>
{
    public GrpcDisallowEvolutionModeInCatalogSchemaMutation Convert(
        DisallowEvolutionModeInCatalogSchemaMutation mutation)
    {
        return new GrpcDisallowEvolutionModeInCatalogSchemaMutation
        {
            EvolutionModes = {mutation.EvolutionModes.Select(EvitaEnumConverter.ToGrpcCatalogEvolutionMode)}
        };
    }

    public DisallowEvolutionModeInCatalogSchemaMutation Convert(
        GrpcDisallowEvolutionModeInCatalogSchemaMutation mutation)
    {
        return new DisallowEvolutionModeInCatalogSchemaMutation(mutation.EvolutionModes
            .Select(EvitaEnumConverter.ToCatalogEvolutionMode).ToArray());
    }
}