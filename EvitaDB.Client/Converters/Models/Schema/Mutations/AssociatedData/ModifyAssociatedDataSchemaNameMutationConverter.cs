using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.AssociatedData;

public class ModifyAssociatedDataSchemaNameMutationConverter : ISchemaMutationConverter<ModifyAssociatedDataSchemaNameMutation, GrpcModifyAssociatedDataSchemaNameMutation>
{
    public GrpcModifyAssociatedDataSchemaNameMutation Convert(ModifyAssociatedDataSchemaNameMutation mutation)
    {
        return new GrpcModifyAssociatedDataSchemaNameMutation
        {
            Name = mutation.Name,
            NewName = mutation.NewName
        };
    }

    public ModifyAssociatedDataSchemaNameMutation Convert(GrpcModifyAssociatedDataSchemaNameMutation mutation)
    {
        return new ModifyAssociatedDataSchemaNameMutation(mutation.Name, mutation.NewName);
    }
}