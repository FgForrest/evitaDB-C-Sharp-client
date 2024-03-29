﻿using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Mutations.Reference;

public class RemoveReferenceGroupMutation : ReferenceMutation
{
    public RemoveReferenceGroupMutation(ReferenceKey referenceKey) : base(referenceKey)
    {
    }

    public override IReference MutateLocal(IEntitySchema entitySchema, IReference? existingValue)
    {
        Assert.IsTrue(
            existingValue is {Dropped: false},
            () => new InvalidMutationException(
                "Cannot remove reference " + ReferenceKey + " - reference doesn't exist!")
        );
        Assert.IsTrue(
            existingValue!.Group is {Dropped: false},
            () => new InvalidMutationException("Cannot remove reference " + ReferenceKey +
                                               " group - no group is currently set!")
        );

        GroupEntityReference? existingReferenceGroup = existingValue.Group;
        return new Structure.Reference(
            entitySchema,
            existingValue.Version + 1,
            existingValue.ReferenceName,
            existingValue.ReferencedPrimaryKey,
            existingValue.ReferencedEntityType,
            existingValue.ReferenceCardinality,
            existingReferenceGroup is {Dropped: false}
                ? new GroupEntityReference(
                    existingReferenceGroup.Type,
                    existingReferenceGroup.PrimaryKey!.Value,
                    existingReferenceGroup.Version + 1,
                    true
                )
                : throw new InvalidMutationException(
                    "Cannot remove reference group - no reference group is set on reference " + ReferenceKey + "!"),
            existingValue.GetAttributeValues(),
            existingValue.ReferencedEntity,
            existingValue.GroupEntity,
            existingValue.Dropped
        );
    }
}