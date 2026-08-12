namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `includingChildrenExcept` constraint is a specification constraint of the <see cref="FacetHaving"/> container that causes all
/// children of the matched hierarchical facet to be excluded from the facet selection (optionally narrowed by the inner
/// filter constraint).
/// Example:
/// <code>
/// facetHaving("categories", entityPrimaryKeyInSet(1), includingChildrenExcept())
/// </code>
/// </summary>
public class FacetIncludingChildrenExcept : AbstractFilterConstraintContainer
{
    private const string ConstraintName = "includingChildrenExcept";

    public IFilterConstraint? Filtering => Children.Length > 0 ? Children[0] : null;

    public FacetIncludingChildrenExcept() : base(ConstraintName, NoArguments)
    {
    }

    public FacetIncludingChildrenExcept(IFilterConstraint filtering) : base(ConstraintName, NoArguments, filtering)
    {
    }

    public new bool Necessary => true;
    public new bool Applicable => true;

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return children.Length > 0 ? new FacetIncludingChildrenExcept(children[0]!) : new FacetIncludingChildrenExcept();
    }
}
