using Client.Models.Schemas;

namespace Client.Models.Data.Mutations.Entity;

public abstract class ParentMutation : ILocalMutation<int?>
{
    public abstract int? MutateLocal(IEntitySchema entitySchema, int? existingValue);
}