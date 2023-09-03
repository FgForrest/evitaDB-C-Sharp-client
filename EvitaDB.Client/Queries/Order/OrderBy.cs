namespace EvitaDB.Client.Queries.Order;

public class OrderBy : AbstractOrderConstraintContainer, IOrderConstraint
{
    public new bool Necessary => Applicable;
    public OrderBy(params IOrderConstraint[] children) : base(children)
    {
    }
    public IOrderConstraint? Child => GetChildrenCount() == 0 ? null : Children[0];
    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint[] children, IConstraint[] additionalChildren)
    {
        return new OrderBy(children);
    }
}