using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class SetEntitySchemaWithHierarchyMutationConverter : ISchemaMutationConverter<SetEntitySchemaWithHierarchyMutation, GrpcSetEntitySchemaWithHierarchyMutation>
{
    public GrpcSetEntitySchemaWithHierarchyMutation Convert(SetEntitySchemaWithHierarchyMutation mutation)
    {
        return new GrpcSetEntitySchemaWithHierarchyMutation
        {
            WithHierarchy = mutation.WithHierarchy
        };
    }

    public SetEntitySchemaWithHierarchyMutation Convert(GrpcSetEntitySchemaWithHierarchyMutation mutation)
    {
        return new SetEntitySchemaWithHierarchyMutation(mutation.WithHierarchy);
    }
}