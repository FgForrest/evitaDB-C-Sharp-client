namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `includingChildren` constraint is a specification constraint of the <see cref="FacetHaving"/> container that causes all
/// children of the matched hierarchical facet to be included in the facet selection (optionally narrowed by the inner
/// filter constraint).
/// Example:
/// <code>
/// facetHaving("categories", entityPrimaryKeyInSet(1), includingChildren())
/// </code>
/// </summary>
public class FacetIncludingChildren : AbstractFilterConstraintContainer, IConstraintWithSuffix
{
    private const string ConstraintName = "includingChildren";
    private const string SuffixHaving = "having";

    public IFilterConstraint? Filtering => Children.Length > 0 ? Children[0] : null;

    /// <summary>
    /// Mirrors Java's `ConstraintWithSuffix`: the argument-less form serializes as `includingChildren()`, while the
    /// filtered form must serialize as `includingChildrenHaving(...)`. These are two distinct constraints in the
    /// evitaQL grammar - `includingChildren` accepts no arguments - so emitting the unsuffixed name for a filtered
    /// instance produces a query the server rejects with a syntax error.
    /// </summary>
    public string? SuffixIfApplied => Children.Length == 0 ? null : SuffixHaving;

    public bool ArgumentImplicitForSuffix(object argument) => false;

    public FacetIncludingChildren() : base(ConstraintName, NoArguments)
    {
    }

    public FacetIncludingChildren(IFilterConstraint filtering) : base(ConstraintName, NoArguments, filtering)
    {
    }

    public new bool Necessary => true;
    public new bool Applicable => true;

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return children.Length > 0 ? new FacetIncludingChildren(children[0]!) : new FacetIncludingChildren();
    }
}
