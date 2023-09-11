using EvitaDB.Client.Models.Data.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Entities;

public class SetParentMutationConverter : ILocalMutationConverter<SetParentMutation, GrpcSetParentMutation>
{
    public GrpcSetParentMutation Convert(SetParentMutation mutation)
    {
        return new GrpcSetParentMutation
        {
            PrimaryKey = mutation.ParentPrimaryKey
        };
    }

    public SetParentMutation Convert(GrpcSetParentMutation mutation)
    {
        return new SetParentMutation(mutation.PrimaryKey);
    }
}