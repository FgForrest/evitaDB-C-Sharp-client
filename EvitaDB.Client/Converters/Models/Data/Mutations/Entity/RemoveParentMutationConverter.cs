using EvitaDB;
using EvitaDB.Client.Models.Data.Mutations.Entity;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Entity;

public class RemoveParentMutationConverter : ILocalMutationConverter<RemoveParentMutation, GrpcRemoveParentMutation>
{
    public GrpcRemoveParentMutation Convert(RemoveParentMutation mutation)
    {
        return new GrpcRemoveParentMutation();
    }

    public RemoveParentMutation Convert(GrpcRemoveParentMutation mutation)
    {
        return new RemoveParentMutation();
    }
}