using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

public class ModifyEntitySchemaNameMutationConverter : ISchemaMutationConverter<ModifyEntitySchemaNameMutation, GrpcModifyEntitySchemaNameMutation>
{
    public GrpcModifyEntitySchemaNameMutation Convert(ModifyEntitySchemaNameMutation mutation)
    {
        return new GrpcModifyEntitySchemaNameMutation
        {
            Name = mutation.Name,
            NewName = mutation.NewName,
            OverwriteTarget = mutation.OverwriteTarget
        };
    }

    public ModifyEntitySchemaNameMutation Convert(GrpcModifyEntitySchemaNameMutation mutation)
    {
        return new ModifyEntitySchemaNameMutation(mutation.Name, mutation.NewName, mutation.OverwriteTarget);
    }
}