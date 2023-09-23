using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.Reference;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.References;

public class SetReferenceGroupMutationConverter : ILocalMutationConverter<SetReferenceGroupMutation, GrpcSetReferenceGroupMutation>
{
    public GrpcSetReferenceGroupMutation Convert(SetReferenceGroupMutation mutation)
    {
        GrpcSetReferenceGroupMutation grpcSetReferenceGroupMutation = new GrpcSetReferenceGroupMutation
        {
            ReferenceName = mutation.ReferenceKey.ReferenceName,
            ReferencePrimaryKey = mutation.ReferenceKey.PrimaryKey,
            GroupPrimaryKey = mutation.GroupPrimaryKey
        };

        if (mutation.GroupType != null)
        {
            grpcSetReferenceGroupMutation.GroupType = mutation.GroupType;
        }

        return grpcSetReferenceGroupMutation;
    }

    public SetReferenceGroupMutation Convert(GrpcSetReferenceGroupMutation mutation)
    {
        return new SetReferenceGroupMutation(
            new ReferenceKey(mutation.ReferenceName, mutation.ReferencePrimaryKey),
            mutation.GroupType,
            mutation.GroupPrimaryKey
        );
    }
}