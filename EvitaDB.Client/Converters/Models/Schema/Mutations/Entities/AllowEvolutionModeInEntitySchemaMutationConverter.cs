using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class AllowEvolutionModeInEntitySchemaMutationConverter : ISchemaMutationConverter<AllowEvolutionModeInEntitySchemaMutation, GrpcAllowEvolutionModeInEntitySchemaMutation>
{
    public GrpcAllowEvolutionModeInEntitySchemaMutation Convert(AllowEvolutionModeInEntitySchemaMutation mutation)
    {
        return new GrpcAllowEvolutionModeInEntitySchemaMutation
        {
            EvolutionModes = { mutation.EvolutionModes.Select(EvitaEnumConverter.ToGrpcEvolutionMode) }
        };
    }

    public AllowEvolutionModeInEntitySchemaMutation Convert(GrpcAllowEvolutionModeInEntitySchemaMutation mutation)
    {
        return new AllowEvolutionModeInEntitySchemaMutation(mutation.EvolutionModes
            .Select(EvitaEnumConverter.ToEvolutionMode).ToArray());
    }
}