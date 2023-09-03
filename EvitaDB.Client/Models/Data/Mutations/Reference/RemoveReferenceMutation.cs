using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Mutations.Reference;

public class RemoveReferenceMutation : ReferenceMutation
{
    public RemoveReferenceMutation(ReferenceKey referenceKey) : base(referenceKey)
    {
    }

    public RemoveReferenceMutation(string referenceName, int primaryKey) : base(new ReferenceKey(referenceName,
        primaryKey))
    {
    }

    public override IReference MutateLocal(IEntitySchema entitySchema, IReference? existingValue)
    {
        Assert.IsTrue(
            existingValue is {Dropped: false},
            () => new InvalidMutationException(
                "Cannot remove reference " + ReferenceKey + " - reference doesn't exist!")
        );
        return new Structure.Reference(
            entitySchema,
            existingValue!.Version + 1,
            existingValue.ReferenceName, existingValue.ReferencedPrimaryKey,
            existingValue.ReferencedEntityType, existingValue.ReferenceCardinality,
            existingValue.Group is not null && !existingValue.Group.Dropped
                ? new GroupEntityReference(existingValue.Group.ReferencedEntity, existingValue.Group.PrimaryKey!.Value,
                    existingValue.Group.Version + 1, true)
                : null,
            existingValue.GetAttributeValues(),
            existingValue.ReferencedEntity,
            existingValue.GroupEntity,
            true
        );
    }
}