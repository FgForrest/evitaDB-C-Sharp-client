namespace Client.Queries.Filter;

public abstract class AbstractFilterConstraintContainer : ConstraintContainer<IFilterConstraint>, IFilterConstraint
{
    public override Type Type => typeof(IFilterConstraint);
    
    protected AbstractFilterConstraintContainer(object[] arguments, params IFilterConstraint[] children) : base(arguments, children) {
    }
    
    protected AbstractFilterConstraintContainer(object[] arguments, IFilterConstraint[] children, params IConstraint[] additionalChildren) : base(arguments, children) {
    }

    protected AbstractFilterConstraintContainer(object argument, params IFilterConstraint[] children) : base(new [] {argument}, children) {
    }

    protected AbstractFilterConstraintContainer(object argument1, object argument2, params IFilterConstraint[] children) : base(new [] {argument1, argument2}, children) {
    }
    
    protected AbstractFilterConstraintContainer(string name, object[] arguments, params IFilterConstraint[] children) : base(name, arguments, children) {
    }

    protected AbstractFilterConstraintContainer(params IFilterConstraint?[] children) : base(children) {
    }
    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }
}