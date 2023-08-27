namespace Client.Queries.Requires;

public abstract class AbstractRequireConstraintContainer : ConstraintContainer<IRequireConstraint>, IRequireConstraint
{
    protected AbstractRequireConstraintContainer(object[] arguments, IRequireConstraint[] children,
        params IConstraint[] additionalChildren) : base(arguments, children, additionalChildren)
    {
    }

    protected AbstractRequireConstraintContainer(IRequireConstraint[] children, params IConstraint[] additionalChildren)
        : base(children, additionalChildren)
    {
    }

    protected AbstractRequireConstraintContainer(object[] arguments, params IRequireConstraint[] children) : base(
        arguments, children)
    {
    }

    protected AbstractRequireConstraintContainer(params IRequireConstraint[] children) : base(children)
    {
    }

    protected AbstractRequireConstraintContainer(string? name, object[] arguments, IRequireConstraint[] children,
        params IConstraint[] additionalChildren) : base(name, arguments, children, additionalChildren)
    {
    }

    protected AbstractRequireConstraintContainer(string? name, params IRequireConstraint[] children) : base(name,
        NoArguments, children)
    {
    }

    protected AbstractRequireConstraintContainer(string name, object[] arguments, params IRequireConstraint[] children)
        : base(name, arguments, children)
    {
    }

    public override Type Type => typeof(IRequireConstraint);

    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }
}