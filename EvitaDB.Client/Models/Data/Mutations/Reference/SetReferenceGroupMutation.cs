using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Mutations.Reference;

public class SetReferenceGroupMutation : ReferenceMutation
{
    public string? GroupType { get; }
    public int GroupPrimaryKey { get; }
    private string? _resolvedGroupType;
    public override Operation Operation => Operation.Upsert;
    
    public SetReferenceGroupMutation(ReferenceKey referenceKey, int groupPrimaryKey) : base(referenceKey) {
        GroupType = null;
        GroupPrimaryKey = groupPrimaryKey;
    }

    public SetReferenceGroupMutation(ReferenceKey referenceKey, string? groupType, int groupPrimaryKey) : base(referenceKey) {
        GroupType = groupType;
        GroupPrimaryKey = groupPrimaryKey;
    }

    public SetReferenceGroupMutation(string referenceName, int referencedEntityPrimaryKey, int groupPrimaryKey) : base(referenceName, referencedEntityPrimaryKey) {
        GroupType = null;
        GroupPrimaryKey = groupPrimaryKey;
    }

    public SetReferenceGroupMutation(string referenceName, int referencedEntityPrimaryKey, string? groupType, int groupPrimaryKey) : base(referenceName, referencedEntityPrimaryKey) {
        GroupType = groupType;
        GroupPrimaryKey = groupPrimaryKey;
    }

    public override IReference MutateLocal(IEntitySchema entitySchema, IReference? existingValue)
    {
        Assert.IsTrue(
            existingValue is {Dropped: false},
            () => new InvalidMutationException(
                "Cannot set reference group " + ReferenceKey + " - reference doesn't exist!")
        );

        GroupEntityReference? existingReferenceGroup = existingValue!.Group;
        if (existingReferenceGroup is {Dropped: false} && existingReferenceGroup.PrimaryKey == GroupPrimaryKey)
        {
            // no change is necessary
            return existingValue;
        }

        return new Structure.Reference(
            entitySchema,
            existingValue.Version + 1,
            existingValue.ReferenceName,
            existingValue.ReferencedPrimaryKey,
            existingValue.ReferencedEntityType,
            existingValue.ReferenceCardinality,
            existingReferenceGroup is not null
                ? new GroupEntityReference(
                    GetGroupType(entitySchema)!,
                    GroupPrimaryKey,
                    existingReferenceGroup.Version + 1,
                    false
                )
                : new GroupEntityReference(
                    GetGroupType(entitySchema)!,
                    GroupPrimaryKey,
                    1,
                    false
                ),
            existingValue.GetAttributeValues(),
            existingValue.ReferencedEntity,
            existingValue.GroupEntity,
            existingValue.Dropped
        );
    }
    
    private string? GetGroupType(IEntitySchema entitySchema) {
        if (_resolvedGroupType == null) {
            if (GroupType == null) {
                IReferenceSchema referenceSchema = entitySchema.GetReferenceOrThrowException(ReferenceKey.ReferenceName);
                _resolvedGroupType = referenceSchema.ReferencedGroupType;
                Assert.IsTrue(
                    _resolvedGroupType != null,
                    () => new InvalidMutationException(
                        "Cannot update reference group - no group type defined in schema and also not provided in the mutation!"
                    )
                    );
            } else {
                _resolvedGroupType = GroupType;
            }
        }
        return _resolvedGroupType;
    }
}
