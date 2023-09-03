using EvitaDB;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.Reference;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Reference;

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