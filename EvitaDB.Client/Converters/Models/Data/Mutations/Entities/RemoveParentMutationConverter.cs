using EvitaDB.Client.Models.Data.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Entities;

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