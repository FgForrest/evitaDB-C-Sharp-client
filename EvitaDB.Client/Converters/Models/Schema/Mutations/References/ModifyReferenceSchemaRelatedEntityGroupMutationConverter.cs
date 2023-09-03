using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class ModifyReferenceSchemaRelatedEntityGroupMutationConverter : ISchemaMutationConverter<
    ModifyReferenceSchemaRelatedEntityGroupMutation, GrpcModifyReferenceSchemaRelatedEntityGroupMutation>
{
    public GrpcModifyReferenceSchemaRelatedEntityGroupMutation Convert(
        ModifyReferenceSchemaRelatedEntityGroupMutation mutation)
    {
        return new GrpcModifyReferenceSchemaRelatedEntityGroupMutation
        {
            Name = mutation.Name,
            ReferencedGroupType = mutation.ReferencedGroupType,
            ReferencedGroupTypeManaged = mutation.ReferencedGroupTypeManaged
        };
    }

    public ModifyReferenceSchemaRelatedEntityGroupMutation Convert(
        GrpcModifyReferenceSchemaRelatedEntityGroupMutation mutation)
    {
        return new ModifyReferenceSchemaRelatedEntityGroupMutation(mutation.Name, mutation.ReferencedGroupType,
            mutation.ReferencedGroupTypeManaged);
    }
}