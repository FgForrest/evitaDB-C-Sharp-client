using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class ModifyReferenceSchemaNameMutationConverter : ISchemaMutationConverter<ModifyReferenceSchemaNameMutation, GrpcModifyReferenceSchemaNameMutation>
{
    public GrpcModifyReferenceSchemaNameMutation Convert(ModifyReferenceSchemaNameMutation mutation)
    {
        return new GrpcModifyReferenceSchemaNameMutation
        {
            Name = mutation.Name,
            NewName = mutation.NewName
        };
    }

    public ModifyReferenceSchemaNameMutation Convert(GrpcModifyReferenceSchemaNameMutation mutation)
    {
        return new ModifyReferenceSchemaNameMutation(mutation.Name, mutation.NewName);
    }
}