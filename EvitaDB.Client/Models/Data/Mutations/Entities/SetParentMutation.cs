using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Entities;

public class SetParentMutation : ParentMutation
{
    public int ParentPrimaryKey { get; }
    public override Operation Operation => Operation.Upsert;
    
    public SetParentMutation(int parentPrimaryKey)
    {
        ParentPrimaryKey = parentPrimaryKey;
    }
    
    public override int? MutateLocal(IEntitySchema entitySchema, int? existingValue)
    {
        return ParentPrimaryKey;
    }
}
