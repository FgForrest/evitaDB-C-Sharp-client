using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.AssociatedData;

public class SetAssociatedDataSchemaConflictResolutionOverrideMutationConverter : ISchemaMutationConverter<
    SetAssociatedDataSchemaConflictResolutionOverrideMutation,
    GrpcSetAssociatedDataSchemaConflictResolutionOverrideMutation>
{
    public GrpcSetAssociatedDataSchemaConflictResolutionOverrideMutation Convert(
        SetAssociatedDataSchemaConflictResolutionOverrideMutation mutation)
    {
        return new GrpcSetAssociatedDataSchemaConflictResolutionOverrideMutation
        {
            Name = mutation.Name,
            ConflictResolutionOverride = EvitaEnumConverter.ToGrpcConflictResolutionOverride(mutation.ConflictResolutionOverride)
        };
    }

    public SetAssociatedDataSchemaConflictResolutionOverrideMutation Convert(
        GrpcSetAssociatedDataSchemaConflictResolutionOverrideMutation mutation)
    {
        return new SetAssociatedDataSchemaConflictResolutionOverrideMutation(
            mutation.Name,
            EvitaEnumConverter.ToConflictResolutionOverride(mutation.ConflictResolutionOverride)
        );
    }
}
