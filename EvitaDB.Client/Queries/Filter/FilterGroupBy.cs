using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// Filtering constraints allow you to select only a few entities from many that exist in the target collection. It's
/// similar to the "where" clause in SQL. FilterGroupBy container might contain one or more sub-constraints, that are
/// combined by logical disjunction (AND).
/// The `filterGroupBy` is equivalent to <see cref="FilterBy"/>, but can be used only within <see cref="FacetSummary"/> container
/// and defines the filter constraints limiting the facet groups returned in facet summary.
/// Example:
/// <code>
/// filterGroupBy(
///    isNotNull("code"),
///    or(
///       equals("code", "ABCD"),
///       startsWith("title", "Knife")
///    )
/// )
/// </code>
/// </summary>
public class FilterGroupBy : AbstractFilterConstraintContainer
{
    private FilterGroupBy()
    {
    }
    
    public FilterGroupBy(params IFilterConstraint?[] children) : base(children)
    {
    }
    
    public new bool Necessary => Applicable;
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        Assert.IsTrue(additionalChildren.Length == 0, "FilterGroupBy doesn't accept other than filtering constraints!");
        return children.Length > 0 ? new FilterGroupBy(children) : new FilterGroupBy();
    }
}
