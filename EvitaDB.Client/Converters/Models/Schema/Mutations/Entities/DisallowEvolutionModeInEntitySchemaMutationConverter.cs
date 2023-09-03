using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class DisallowEvolutionModeInEntitySchemaMutationConverter : ISchemaMutationConverter<DisallowEvolutionModeInEntitySchemaMutation, GrpcDisallowEvolutionModeInEntitySchemaMutation>
{
    public GrpcDisallowEvolutionModeInEntitySchemaMutation Convert(DisallowEvolutionModeInEntitySchemaMutation mutation)
    {
        return new GrpcDisallowEvolutionModeInEntitySchemaMutation
        {
            EvolutionModes = {mutation.EvolutionModes.Select(EvitaEnumConverter.ToGrpcEvolutionMode)}
        };
    }

    public DisallowEvolutionModeInEntitySchemaMutation Convert(GrpcDisallowEvolutionModeInEntitySchemaMutation mutation)
    {
        return new DisallowEvolutionModeInEntitySchemaMutation(mutation.EvolutionModes
            .Select(EvitaEnumConverter.ToEvolutionMode).ToArray());
    }
}