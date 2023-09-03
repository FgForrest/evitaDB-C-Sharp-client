using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Entity;

public class SetParentMutation : ParentMutation
{
    public int ParentPrimaryKey { get; }
    
    public SetParentMutation(int parentPrimaryKey)
    {
        ParentPrimaryKey = parentPrimaryKey;
    }
    
    public override int? MutateLocal(IEntitySchema entitySchema, int? existingValue)
    {
        return ParentPrimaryKey;
    }
}