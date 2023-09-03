namespace EvitaDB.Client.Queries.Order;

public class OrderGroupBy : AbstractOrderConstraintContainer
{
    public new bool Necessary => Applicable;
    
    public OrderGroupBy(params IOrderConstraint[] children) : base(children)
    {
    }
    
    public IOrderConstraint? Child => GetChildrenCount() == 0 ? null : Children[0];

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children, IConstraint[] additionalChildren)
    {
        return new OrderGroupBy(children);
    }
}