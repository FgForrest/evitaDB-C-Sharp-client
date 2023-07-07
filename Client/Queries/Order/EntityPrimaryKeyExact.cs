namespace Client.Queries.Order;

public class EntityPrimaryKeyExact : AbstractOrderConstraintLeaf
{
    private EntityPrimaryKeyExact(params object[] args) : base(args)
    {
    }
    
    public EntityPrimaryKeyExact(params int?[] primaryKeys) : base(primaryKeys)
    {
    }
    
    public int[] PrimaryKeys => Arguments.Select(x=> (int) x!).ToArray();
}