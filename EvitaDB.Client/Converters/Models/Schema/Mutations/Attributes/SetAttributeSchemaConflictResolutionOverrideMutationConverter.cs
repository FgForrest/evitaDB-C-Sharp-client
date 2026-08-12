using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaConflictResolutionOverrideMutationConverter : ISchemaMutationConverter<
    SetAttributeSchemaConflictResolutionOverrideMutation, GrpcSetAttributeSchemaConflictResolutionOverrideMutation>
{
    public GrpcSetAttributeSchemaConflictResolutionOverrideMutation Convert(
        SetAttributeSchemaConflictResolutionOverrideMutation mutation)
    {
        return new GrpcSetAttributeSchemaConflictResolutionOverrideMutation
        {
            Name = mutation.Name,
            ConflictResolutionOverride = EvitaEnumConverter.ToGrpcConflictResolutionOverride(mutation.ConflictResolutionOverride)
        };
    }

    public SetAttributeSchemaConflictResolutionOverrideMutation Convert(
        GrpcSetAttributeSchemaConflictResolutionOverrideMutation mutation)
    {
        return new SetAttributeSchemaConflictResolutionOverrideMutation(
            mutation.Name,
            EvitaEnumConverter.ToConflictResolutionOverride(mutation.ConflictResolutionOverride)
        );
    }
}
