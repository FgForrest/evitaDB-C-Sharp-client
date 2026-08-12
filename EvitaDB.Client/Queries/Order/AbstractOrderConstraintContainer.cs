namespace EvitaDB.Client.Queries.Order;

public abstract class AbstractOrderConstraintContainer : ConstraintContainer<IOrderConstraint>, IOrderConstraint
{
    protected AbstractOrderConstraintContainer(object?[] arguments, params IOrderConstraint?[] children) : base(arguments,
        children)
    {
    }

    protected AbstractOrderConstraintContainer(object? argument, params IOrderConstraint?[] children) : base(
        new[] {argument}, children)
    {
    }

    protected AbstractOrderConstraintContainer(params IOrderConstraint?[] children) : base(children)
    {
    }

    protected AbstractOrderConstraintContainer(string? name, object?[] arguments, IOrderConstraint?[] children,
        params IConstraint?[] additionalChildren)
        : base(name, arguments, children, additionalChildren)
    {
    }

    protected AbstractOrderConstraintContainer(object?[] arguments, IOrderConstraint?[] children,
        params IConstraint?[] additionalChildren)
        : base(arguments, children, additionalChildren)
    {
    }

    public override Type Type => typeof(IOrderConstraint);

    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }
}