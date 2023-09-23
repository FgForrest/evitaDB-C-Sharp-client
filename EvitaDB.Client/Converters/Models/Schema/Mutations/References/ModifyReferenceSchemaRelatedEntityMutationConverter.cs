using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class ModifyReferenceSchemaRelatedEntityMutationConverter : ISchemaMutationConverter<
    ModifyReferenceSchemaRelatedEntityMutation, GrpcModifyReferenceSchemaRelatedEntityMutation>
{
    public GrpcModifyReferenceSchemaRelatedEntityMutation Convert(ModifyReferenceSchemaRelatedEntityMutation mutation)
    {
        return new GrpcModifyReferenceSchemaRelatedEntityMutation
        {
            Name = mutation.Name,
            ReferencedEntityType = mutation.ReferencedEntityType,
            ReferencedEntityTypeManaged = mutation.ReferencedEntityTypeManaged
        };
    }

    public ModifyReferenceSchemaRelatedEntityMutation Convert(GrpcModifyReferenceSchemaRelatedEntityMutation mutation)
    {
        return new ModifyReferenceSchemaRelatedEntityMutation(mutation.Name, mutation.ReferencedEntityType,
            mutation.ReferencedEntityTypeManaged);
    }
}