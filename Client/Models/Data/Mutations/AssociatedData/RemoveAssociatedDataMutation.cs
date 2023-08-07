using Client.Exceptions;
using Client.Models.Schemas;
using Client.Utils;

namespace Client.Models.Data.Mutations.AssociatedData;

public class RemoveAssociatedDataMutation : AssociatedDataMutation
{
    public RemoveAssociatedDataMutation(AssociatedDataKey associatedDataKey) : base(associatedDataKey)
    {
    }

    public override AssociatedDataValue MutateLocal(IEntitySchema entitySchema, AssociatedDataValue? existingValue)
    {
        Assert.IsTrue(
            existingValue is {Dropped: false},
            () => new InvalidMutationException(
                "Cannot remove " + AssociatedDataKey.AssociatedDataName +
                " associated data - it doesn't exist!"
            )
        );
        return new AssociatedDataValue(existingValue!.Version + 1, existingValue.Key, existingValue.Value, true);
    }
}