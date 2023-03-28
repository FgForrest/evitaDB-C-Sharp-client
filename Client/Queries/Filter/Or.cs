namespace Client.Queries.Filter;

public class Or : AbstractFilterConstraintContainer
{
    public Or(params IFilterConstraint?[] children) : base(children)
    {
    }
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children, IConstraint[] additionalChildren)
    {
        return new Or(children);
    }
}