using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.AssociatedData;

public abstract class AssociatedDataMutation : ILocalMutation<AssociatedDataValue>
{
    public abstract Operation Operation { get; }
    public AssociatedDataKey AssociatedDataKey { get; }
    
    protected AssociatedDataMutation(AssociatedDataKey associatedDataKey)
    {
        AssociatedDataKey = associatedDataKey;
    }
    
    public abstract AssociatedDataValue MutateLocal(IEntitySchema entitySchema, AssociatedDataValue? existingValue);
}
