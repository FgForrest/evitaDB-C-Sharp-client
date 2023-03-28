namespace Client.Queries.Order;

public abstract class AbstractOrderConstraintContainer : ConstraintContainer<IOrderConstraint>, IOrderConstraint
{
    protected AbstractOrderConstraintContainer(object[] arguments, params IOrderConstraint[] children) : base(arguments, children) {
    }
    
    protected AbstractOrderConstraintContainer(object argument, params IOrderConstraint[] children) : base(new [] {argument}, children) {
    }
    
    protected AbstractOrderConstraintContainer(params IOrderConstraint[] children) : base(children) {
    }

    public override Type Type => typeof(IOrderConstraint);
    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }
}