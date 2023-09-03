namespace EvitaDB.Client.Queries.Order;

public class ReferenceProperty : AbstractOrderConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;
    public new bool Necessary => Children.Length >= 1;

    private ReferenceProperty(object[] arguments, params IOrderConstraint[] children) : base(arguments, children)
    {
    }

    public ReferenceProperty(string referenceName, params IOrderConstraint[] children) : base(referenceName, children)
    {
    }

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint[] children,
        IConstraint[] additionalChildren)
    {
        return new ReferenceProperty(ReferenceName, children);
    }
}