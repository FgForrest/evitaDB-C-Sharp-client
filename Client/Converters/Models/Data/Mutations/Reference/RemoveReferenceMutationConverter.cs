using Client.Models.Data;
using Client.Models.Data.Mutations.Reference;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.Reference;

public class RemoveReferenceMutationConverter : ILocalMutationConverter<RemoveReferenceMutation, GrpcRemoveReferenceMutation>
{
    public GrpcRemoveReferenceMutation Convert(RemoveReferenceMutation mutation)
    {
        return new GrpcRemoveReferenceMutation
        {
            ReferenceName = mutation.ReferenceKey.ReferenceName,
            ReferencePrimaryKey = mutation.ReferenceKey.PrimaryKey
        };
    }

    public RemoveReferenceMutation Convert(GrpcRemoveReferenceMutation mutation)
    {
        return new RemoveReferenceMutation(new ReferenceKey(mutation.ReferenceName, mutation.ReferencePrimaryKey));
    }
}