namespace Client.Queries.Filter;

public class EntityPrimaryKeyInSet : AbstractFilterConstraintLeaf
{
    private EntityPrimaryKeyInSet(params object[] arguments) : base(arguments)
    {
        
    }

    public EntityPrimaryKeyInSet(params int[] primaryKeys) : base(primaryKeys)
    {
    }

    public int[] GetPrimaryKey() => Arguments.Select(Convert.ToInt32).ToArray();
}