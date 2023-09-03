namespace EvitaDB.Client.Queries.Filter;

public class EntityPrimaryKeyInSet : AbstractFilterConstraintLeaf
{
    private EntityPrimaryKeyInSet(params object[] arguments) : base(arguments)
    {
        
    }

    public EntityPrimaryKeyInSet(params int[] primaryKeys) : base(primaryKeys.Cast<object>().ToArray())
    {
    }

    public int[] PrimaryKeys => Arguments.Select(Convert.ToInt32).ToArray();
}