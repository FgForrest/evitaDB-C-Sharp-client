using Client.Models.Schemas.Mutations.AssociatedData;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.AssociatedData;

public class ModifyAssociatedDataSchemaDescriptionMutationConverter : ISchemaMutationConverter<ModifyAssociatedDataSchemaDescriptionMutation, GrpcModifyAssociatedDataSchemaDescriptionMutation>
{
    public GrpcModifyAssociatedDataSchemaDescriptionMutation Convert(ModifyAssociatedDataSchemaDescriptionMutation mutation)
    {
        return new GrpcModifyAssociatedDataSchemaDescriptionMutation
        {
            Name = mutation.Name,
            Description = mutation.Description
        };
    }

    public ModifyAssociatedDataSchemaDescriptionMutation Convert(GrpcModifyAssociatedDataSchemaDescriptionMutation mutation)
    {
        return new ModifyAssociatedDataSchemaDescriptionMutation(mutation.Name, mutation.Description);
    }
}