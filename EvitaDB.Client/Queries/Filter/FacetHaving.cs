namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `facetHaving` filtering constraint is typically placed inside the <see cref="UserFilter"/> constraint container and
/// represents the user's request to drill down the result set by a particular facet. The `facetHaving` constraint works
/// exactly like the referenceHaving constraint, but works in conjunction with the facetSummary requirement to correctly
/// calculate the facet statistics and impact predictions. When used outside the userFilter constraint container,
/// the `facetHaving` constraint behaves like the <see cref="ReferenceHaving"/> constraint.
/// Example:
/// <code>
/// userFilter(
///   facetHaving(
///     "brand",
///     entityHaving(
///       attributeInSet("code", "amazon")
///     )
///   )
/// )
/// </code>
/// </summary>
public class FacetHaving : AbstractFilterConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;
    public new bool Necessary => Arguments.Length == 1 && Children.Length > 0;
    
    private FacetHaving(object?[] arguments, params IFilterConstraint?[] children) : base(arguments, children)
    {
    }
    
    private FacetHaving(string referenceName) : base(referenceName)
    {
    }

    public FacetHaving(string referenceName, params IFilterConstraint?[] filter) : base(new object[] {referenceName},
        filter)
    {
    }
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return children.Length == 0 ? new FacetHaving(ReferenceName) : new FacetHaving(ReferenceName, children);
    }
}
