using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Entities;

public abstract class ParentMutation : ILocalMutation<int?>
{
    public abstract Operation Operation { get; }
    public abstract int? MutateLocal(IEntitySchema entitySchema, int? existingValue);
}
