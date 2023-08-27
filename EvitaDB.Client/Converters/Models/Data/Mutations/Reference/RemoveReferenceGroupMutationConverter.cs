using Client.Models.Data;
using Client.Models.Data.Mutations.Reference;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.Reference;

public class
    RemoveReferenceGroupMutationConverter : ILocalMutationConverter<RemoveReferenceGroupMutation,
        GrpcRemoveReferenceGroupMutation>
{
    public GrpcRemoveReferenceGroupMutation Convert(RemoveReferenceGroupMutation mutation)
    {
        return new GrpcRemoveReferenceGroupMutation
        {
            ReferenceName = mutation.ReferenceKey.ReferenceName,
            ReferencePrimaryKey = mutation.ReferenceKey.PrimaryKey
        };
    }

    public RemoveReferenceGroupMutation Convert(GrpcRemoveReferenceGroupMutation mutation)
    {
        return new RemoveReferenceGroupMutation(new ReferenceKey(mutation.ReferenceName, mutation.ReferencePrimaryKey));
    }
}