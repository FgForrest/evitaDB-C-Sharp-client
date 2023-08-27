using Client.Models.Data.Mutations.Entity;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.Entity;

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