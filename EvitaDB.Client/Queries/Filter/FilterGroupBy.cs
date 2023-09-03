using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

public class FilterGroupBy : AbstractFilterConstraintContainer
{
    private FilterGroupBy()
    {
    }
    
    public FilterGroupBy(params IFilterConstraint[] children) : base(children)
    {
    }
    
    public new bool Necessary => Applicable;
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children, IConstraint[] additionalChildren)
    {
        Assert.IsTrue(additionalChildren.Length == 0, "FilterGroupBy doesn't accept other than filtering constraints!");
        return children.Length > 0 ? new FilterGroupBy(children) : new FilterGroupBy();
    }
}