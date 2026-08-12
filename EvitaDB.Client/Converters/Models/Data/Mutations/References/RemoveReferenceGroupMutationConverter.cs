using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.Reference;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.References;

public class
    RemoveReferenceGroupMutationConverter : ILocalMutationConverter<RemoveReferenceGroupMutation,
        GrpcRemoveReferenceGroupMutation>
{
    public GrpcRemoveReferenceGroupMutation Convert(RemoveReferenceGroupMutation mutation)
    {
        return new GrpcRemoveReferenceGroupMutation
        {
            ReferenceName = mutation.ReferenceKey.ReferenceName,
            ReferencePrimaryKey = mutation.ReferenceKey.PrimaryKey,
            InternalPrimaryKey = mutation.InternalPrimaryKey
        };
    }

    public RemoveReferenceGroupMutation Convert(GrpcRemoveReferenceGroupMutation mutation)
    {
        return new RemoveReferenceGroupMutation(new ReferenceKey(mutation.ReferenceName, mutation.ReferencePrimaryKey))
        {
            InternalPrimaryKey = mutation.InternalPrimaryKey
        };
    }
}