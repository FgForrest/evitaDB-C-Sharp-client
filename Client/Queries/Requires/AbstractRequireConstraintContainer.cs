namespace Client.Queries.Requires;

public abstract class AbstractRequireConstraintContainer : ConstraintContainer<IRequireConstraint>, IRequireConstraint
{
    protected AbstractRequireConstraintContainer(object[] arguments, IRequireConstraint[] children, params IConstraint[] additionalChildren) : base(arguments, children, additionalChildren) {
    }
    
    protected AbstractRequireConstraintContainer(IRequireConstraint[] children, params IConstraint[] additionalChildren) : base(children, additionalChildren) {
    }
    
    protected AbstractRequireConstraintContainer(object[] arguments, params IRequireConstraint[] additionalChildren) : base(arguments, additionalChildren) {
    }
    
    protected AbstractRequireConstraintContainer(params IRequireConstraint[] additionalChildren) : base(additionalChildren) {
    }

    public override Type Type => typeof(IRequireConstraint);
    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }
}