using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class SetReferenceSchemaConflictResolutionOverrideMutationConverter : ISchemaMutationConverter<
    SetReferenceSchemaConflictResolutionOverrideMutation, GrpcSetReferenceSchemaConflictResolutionOverrideMutation>
{
    public GrpcSetReferenceSchemaConflictResolutionOverrideMutation Convert(
        SetReferenceSchemaConflictResolutionOverrideMutation mutation)
    {
        return new GrpcSetReferenceSchemaConflictResolutionOverrideMutation
        {
            Name = mutation.Name,
            ConflictResolutionOverride = EvitaEnumConverter.ToGrpcConflictResolutionOverride(mutation.ConflictResolutionOverride)
        };
    }

    public SetReferenceSchemaConflictResolutionOverrideMutation Convert(
        GrpcSetReferenceSchemaConflictResolutionOverrideMutation mutation)
    {
        return new SetReferenceSchemaConflictResolutionOverrideMutation(
            mutation.Name,
            EvitaEnumConverter.ToConflictResolutionOverride(mutation.ConflictResolutionOverride)
        );
    }
}
