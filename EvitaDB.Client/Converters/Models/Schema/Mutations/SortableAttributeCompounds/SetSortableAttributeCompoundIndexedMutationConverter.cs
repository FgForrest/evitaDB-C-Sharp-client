using EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.SortableAttributeCompounds;

/// <summary>
/// Until the C# model supports entity scopes, the boolean indexed flag maps to indexing in the live scope.
/// </summary>
public class SetSortableAttributeCompoundIndexedMutationConverter : ISchemaMutationConverter<
    SetSortableAttributeCompoundIndexedMutation, GrpcSetSortableAttributeCompoundIndexedMutation>
{
    public GrpcSetSortableAttributeCompoundIndexedMutation Convert(SetSortableAttributeCompoundIndexedMutation mutation)
    {
        GrpcSetSortableAttributeCompoundIndexedMutation grpcMutation = new()
        {
            Name = mutation.Name
        };
        if (mutation.Indexed)
        {
            grpcMutation.IndexedInScopes.Add(GrpcEntityScope.ScopeLive);
        }
        return grpcMutation;
    }

    public SetSortableAttributeCompoundIndexedMutation Convert(GrpcSetSortableAttributeCompoundIndexedMutation mutation)
    {
        return new SetSortableAttributeCompoundIndexedMutation(mutation.Name, mutation.IndexedInScopes.Count > 0);
    }
}
