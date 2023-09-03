using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Mutations.Reference;

public class SetReferenceGroupMutation : ReferenceMutation
{
    public string? GroupType { get; }
    public int GroupPrimaryKey { get; }
    private string? _resolvedGroupType;
    
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
        throw new NotImplementedException();
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