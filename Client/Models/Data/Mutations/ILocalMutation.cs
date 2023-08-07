using Client.Models.Mutations;
using Client.Models.Schemas;

namespace Client.Models.Data.Mutations;

public interface ILocalMutation : IMutation
{
}

public interface ILocalMutation<T> : ILocalMutation
{
    T MutateLocal(IEntitySchema entitySchema, T? existingValue);
}