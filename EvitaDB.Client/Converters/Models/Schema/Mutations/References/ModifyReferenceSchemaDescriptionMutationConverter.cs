using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class ModifyReferenceSchemaDescriptionMutationConverter : ISchemaMutationConverter<ModifyReferenceSchemaDescriptionMutation, GrpcModifyReferenceSchemaDescriptionMutation>
{
    public GrpcModifyReferenceSchemaDescriptionMutation Convert(ModifyReferenceSchemaDescriptionMutation mutation)
    {
        return new GrpcModifyReferenceSchemaDescriptionMutation
        {
            Name = mutation.Name,
            Description = mutation.Description
        };
    }

    public ModifyReferenceSchemaDescriptionMutation Convert(GrpcModifyReferenceSchemaDescriptionMutation mutation)
    {
        return new ModifyReferenceSchemaDescriptionMutation(mutation.Name, mutation.Description);
    }
}