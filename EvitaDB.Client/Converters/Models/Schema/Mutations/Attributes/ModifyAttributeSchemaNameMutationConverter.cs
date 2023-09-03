using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class ModifyAttributeSchemaNameMutationConverter : ISchemaMutationConverter<ModifyAttributeSchemaNameMutation, GrpcModifyAttributeSchemaNameMutation>
{
    public GrpcModifyAttributeSchemaNameMutation Convert(ModifyAttributeSchemaNameMutation mutation)
    {
        return new GrpcModifyAttributeSchemaNameMutation
        {
            Name = mutation.Name,
            NewName = mutation.NewName
        };
    }

    public ModifyAttributeSchemaNameMutation Convert(GrpcModifyAttributeSchemaNameMutation mutation)
    {
        return new ModifyAttributeSchemaNameMutation(mutation.Name, mutation.NewName);
    }
}