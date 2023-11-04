namespace EvitaDB.Client.Queries.Order;

public class EntityProperty : AbstractOrderConstraintContainer
{
    public new bool Necessary => Children.Length >= 1;

    private EntityProperty(object[] arguments, params IOrderConstraint[] children) : base(arguments, children) {
    }
    
    public EntityProperty(params IOrderConstraint?[] children) : base(children) {
    }
    
    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new EntityProperty(children);
    }
}