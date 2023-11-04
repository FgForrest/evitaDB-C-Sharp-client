namespace EvitaDB.Client.Queries.Order;

public class EntityGroupProperty : AbstractOrderConstraintContainer
{
    public new bool Necessary => Children.Length >= 1;

    private EntityGroupProperty(object?[] arguments, params IOrderConstraint?[] children) : base(arguments, children)
    {
    }
    
    public EntityGroupProperty(params IOrderConstraint?[] children) : base(children)
    {
    }
    
    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new EntityGroupProperty(children);
    }
}