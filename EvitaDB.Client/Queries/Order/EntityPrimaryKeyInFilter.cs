namespace EvitaDB.Client.Queries.Order;

public class EntityPrimaryKeyInFilter : AbstractOrderConstraintLeaf
{
    private EntityPrimaryKeyInFilter(params object?[] args) : base(args)
    {
    }
    
    public EntityPrimaryKeyInFilter() : base()
    {
    }

    public new bool Applicable => true;
}