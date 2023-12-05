namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// Filtering constraints allow you to select only a few entities from many that exist in the target collection. It's
/// similar to the "where" clause in SQL. FilterBy container might contain one or more sub-constraints, that are combined
/// by logical disjunction (AND).
/// Example:
/// <code>
/// filterBy(
///    isNotNull("code"),
///    or(
///       equals("code", "ABCD"),
///       startsWith("title", "Knife")
///    )
/// )
/// </code>
/// </summary>
public class FilterBy : AbstractFilterConstraintContainer
{
    private FilterBy() : base() {
    }
    
    public FilterBy(params IFilterConstraint?[] children) : base(children) {
    }

    public new bool Necessary => Applicable;

    public IFilterConstraint? Child => GetChildrenCount() == 0 ? null : Children[0];

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return children.Length > 0 ? new FilterBy(children[0]) : new FilterBy();

    }
}
