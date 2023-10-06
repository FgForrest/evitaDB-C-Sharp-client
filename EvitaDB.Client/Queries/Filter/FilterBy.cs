namespace EvitaDB.Client.Queries.Filter;

public class FilterBy : AbstractFilterConstraintContainer
{
    private FilterBy() : base() {
    }
    
    public FilterBy(params IFilterConstraint[] children) : base(children) {
    }

    public bool Necessary => Applicable;

    public IFilterConstraint? Child => GetChildrenCount() == 0 ? null : Children[0];

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children, IConstraint[] additionalChildren)
    {
        return children.Length > 0 ? new FilterBy(children[0]) : new FilterBy();

    }
}