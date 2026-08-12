using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class SetEntitySchemaWithHierarchyMutationConverter : ISchemaMutationConverter<SetEntitySchemaWithHierarchyMutation, GrpcSetEntitySchemaWithHierarchyMutation>
{
    public GrpcSetEntitySchemaWithHierarchyMutation Convert(SetEntitySchemaWithHierarchyMutation mutation)
    {
        GrpcSetEntitySchemaWithHierarchyMutation grpcMutation = new()
        {
            WithHierarchy = mutation.WithHierarchy
        };
        if (mutation.WithHierarchy)
        {
            // the server requires at least one indexing scope when the entity is hierarchical; until the C# model
            // supports entity scopes explicitly, the hierarchy is indexed in the live scope
            grpcMutation.IndexedInScopes.Add(GrpcEntityScope.ScopeLive);
        }
        return grpcMutation;
    }

    public SetEntitySchemaWithHierarchyMutation Convert(GrpcSetEntitySchemaWithHierarchyMutation mutation)
    {
        return new SetEntitySchemaWithHierarchyMutation(mutation.WithHierarchy);
    }
}