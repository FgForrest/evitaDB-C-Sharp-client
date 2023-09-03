using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Mutations.Entity;

public class RemoveParentMutation : ParentMutation
{
    public override int? MutateLocal(IEntitySchema entitySchema, int? existingValue)
    {
        Assert.IsTrue(
            existingValue != null,
            () => new InvalidMutationException("Cannot remove parent that is not present!")
            );
        return null;
    }
}