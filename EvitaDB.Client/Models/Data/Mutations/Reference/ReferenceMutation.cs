using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Reference;

public abstract class ReferenceMutation : ILocalMutation<IReference>
{
    public ReferenceKey ReferenceKey { get; }
    
    protected ReferenceMutation(ReferenceKey referenceKey)
    {
        ReferenceKey = referenceKey;
    }
    
    protected ReferenceMutation(string referenceName, int primaryKey) : this(new ReferenceKey(referenceName, primaryKey))
    {
    }
    
    public abstract IReference MutateLocal(IEntitySchema entitySchema, IReference? existingValue);
}