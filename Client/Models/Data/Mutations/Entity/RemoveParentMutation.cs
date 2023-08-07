using Client.Exceptions;
using Client.Models.Schemas;
using Client.Utils;

namespace Client.Models.Data.Mutations.Entity;

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