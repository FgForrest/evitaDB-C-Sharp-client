namespace Client.Queries.Filter;

public class Not : AbstractFilterConstraintContainer
{
    private Not()
    {
        
    }
    
    public Not(IFilterConstraint children) : base(children)
    {
    }

    public bool Necessary => Children.Length > 0;
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children, IConstraint[] additionalChildren)
    {
        return children.Length == 0 ? new Not() : new Not(children[0]);
    }
}