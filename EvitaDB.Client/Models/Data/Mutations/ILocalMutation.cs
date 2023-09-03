using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations;

public interface ILocalMutation : IMutation
{
}

public interface ILocalMutation<T> : ILocalMutation
{
    T MutateLocal(IEntitySchema entitySchema, T? existingValue);
}