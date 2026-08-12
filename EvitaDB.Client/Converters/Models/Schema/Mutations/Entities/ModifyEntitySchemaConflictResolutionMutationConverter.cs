using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class ModifyEntitySchemaConflictResolutionMutationConverter : ISchemaMutationConverter<
    ModifyEntitySchemaConflictResolutionMutation, GrpcModifyEntitySchemaConflictResolutionMutation>
{
    public GrpcModifyEntitySchemaConflictResolutionMutation Convert(
        ModifyEntitySchemaConflictResolutionMutation mutation)
    {
        GrpcModifyEntitySchemaConflictResolutionMutation grpcMutation = new();
        GrpcConflictResolution? conflictResolution = EvitaEnumConverter.ToGrpcConflictResolution(mutation.ConflictResolution);
        if (conflictResolution is not null)
        {
            grpcMutation.ConflictResolution = conflictResolution;
        }
        return grpcMutation;
    }

    public ModifyEntitySchemaConflictResolutionMutation Convert(
        GrpcModifyEntitySchemaConflictResolutionMutation mutation)
    {
        return new ModifyEntitySchemaConflictResolutionMutation(
            EvitaEnumConverter.ToConflictResolution(mutation.ConflictResolution)
        );
    }
}
