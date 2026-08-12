using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `facetGroupsExclusivity` requirement marks the matching facet groups as exclusive - i.e. only a single facet
/// of the group (or a single group, depending on the passed <see cref="FacetGroupRelationLevel"/>) may be selected
/// at a time.
/// Example:
/// <code>
/// facetGroupsExclusivity("parameterType", filterBy(entityPrimaryKeyInSet(1, 8, 15)))
/// </code>
/// </summary>
public class FacetGroupsExclusivity : AbstractRequireConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;

    public FacetGroupRelationLevel FacetGroupRelationLevel =>
        Arguments.OfType<FacetGroupRelationLevel>().FirstOrDefault(FacetGroupRelationLevel.WithDifferentFacetsInGroup);

    public FilterBy? FacetGroups => AdditionalChildren.OfType<FilterBy>().FirstOrDefault();

    private FacetGroupsExclusivity(object?[] arguments, params IConstraint?[] additionalChildren) : base(arguments,
        NoChildren, additionalChildren)
    {
        foreach (IConstraint? child in additionalChildren)
        {
            Assert.IsPremiseValid(child is FilterBy,
                "Only FilterBy constraints are allowed in FacetGroupsExclusivity.");
        }
    }

    public FacetGroupsExclusivity(string referenceName, FilterBy? filterBy) : base(
        new object[] {referenceName}, NoChildren, filterBy)
    {
    }

    public FacetGroupsExclusivity(string referenceName, FacetGroupRelationLevel? facetGroupRelationLevel,
        FilterBy? filterBy) : base(
        new object[] {referenceName, facetGroupRelationLevel ?? FacetGroupRelationLevel.WithDifferentFacetsInGroup},
        NoChildren, filterBy)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        Assert.IsPremiseValid(children.Length == 0, "Children must be empty.");
        return new FacetGroupsExclusivity(Arguments, additionalChildren);
    }
}
