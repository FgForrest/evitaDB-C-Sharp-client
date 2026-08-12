namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `anyHaving` hierarchy specification constraint accepts the hierarchy node when at least one of its children
/// (or the node itself) satisfies the inner filter. It complements <see cref="HierarchyHaving"/> which requires the
/// node itself to satisfy the filter.
/// Example:
/// <code>
/// hierarchyWithin("categories", attributeEquals("code", "accessories"), anyHaving(attributeEquals("status", "ACTIVE")))
/// </code>
/// </summary>
public class HierarchyAnyHaving : AbstractFilterConstraintContainer, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "anyHaving";

    public IFilterConstraint?[] Filtering => Children;

    private HierarchyAnyHaving(object?[] arguments, params IFilterConstraint?[] children) : base(ConstraintName,
        arguments, children)
    {
    }

    public HierarchyAnyHaving(params IFilterConstraint?[] filtering) : base(ConstraintName, NoArguments, filtering)
    {
    }

    public new bool Necessary => Applicable;

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new HierarchyAnyHaving(children);
    }
}
