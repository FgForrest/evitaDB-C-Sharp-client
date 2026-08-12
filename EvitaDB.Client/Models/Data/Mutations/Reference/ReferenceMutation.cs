using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Reference;

public abstract class ReferenceMutation : ILocalMutation<IReference>
{
    public ReferenceKey ReferenceKey { get; }

    /// <summary>
    /// Internal primary key of the reference occurrence the mutation targets. Only meaningful for references
    /// with duplicate cardinality (`*_WITH_DUPLICATES`) where multiple occurrences of the same referenced entity
    /// may exist; zero/unset otherwise. Assigned by the server - see
    /// `GrpcUpsertEntityResponse.entityReferenceWithAssignedPrimaryKeys` for the reassignment contract.
    /// </summary>
    public int InternalPrimaryKey { get; init; }

    public abstract Operation Operation { get; }
    
    protected ReferenceMutation(ReferenceKey referenceKey)
    {
        ReferenceKey = referenceKey;
    }
    
    protected ReferenceMutation(string referenceName, int primaryKey) : this(new ReferenceKey(referenceName, primaryKey))
    {
    }
    
    public abstract IReference MutateLocal(IEntitySchema entitySchema, IReference? existingValue);
}
