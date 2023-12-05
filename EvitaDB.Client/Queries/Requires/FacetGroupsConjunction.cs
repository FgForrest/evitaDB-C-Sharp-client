using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// This `facetGroupsConjunction` require allows specifying inter-facet relation inside facet groups of certain primary ids.
/// First mandatory argument specifies entity type of the facet group, secondary argument allows to define one more facet
/// group ids which inner facets should be considered conjunctive.
/// This require constraint changes default behaviour stating that all facets inside same facet group are combined by OR
/// relation (eg. disjunction). Constraint has sense only when [facet](#facet) constraint is part of the query.
/// Example:
/// <code>
/// query(
///    entities("product"),
///    filterBy(
///       userFilter(
///          facet("group", 1, 2),
///          facet(
///             "parameterType",
///             entityPrimaryKeyInSet(11, 12, 22)
///          )
///       )
///    ),
///    require(
///       facetGroupsConjunction("parameterType", 1, 8, 15)
///    )
/// )
/// </code>
/// This statement means, that facets in `parameterType` groups `1`, `8`, `15` will be joined with boolean AND relation when
/// selected.
/// Let's have this facet/group situation:
/// Color `parameterType` (group id: 1):
/// - blue (facet id: 11)
/// - red (facet id: 12)
/// Size `parameterType` (group id: 2):
/// - small (facet id: 21)
/// - large (facet id: 22)
/// Flags `tag` (group id: 3):
/// - action products (facet id: 31)
/// - new products (facet id: 32)
/// When user selects facets: blue (11), red (12) by default relation would be: get all entities that have facet blue(11) OR
/// facet red(12). If require `facetGroupsConjunction('parameterType', 1)` is passed in the query filtering condition will
/// be composed as: blue(11) AND red(12)
/// </summary>
public class FacetGroupsConjunction : AbstractRequireConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;

    public FilterBy? FacetGroups => AdditionalChildren.OfType<FilterBy>().FirstOrDefault();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 0;

    private FacetGroupsConjunction(object?[] arguments, params IConstraint?[] additionalChildren) : base(arguments,
        NoChildren, additionalChildren)
    {
        foreach (IConstraint? child in additionalChildren)
        {
            Assert.IsPremiseValid(child is FilterBy,
                "Only FilterBy constraints are allowed in FacetGroupsConjunction.");
        }
    }

    public FacetGroupsConjunction(string referenceName, FilterBy? filterBy) : base(new object[] {referenceName},
        NoChildren, filterBy)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        Assert.IsPremiseValid(children.Length == 0, "Children must be empty.");
        return new FacetGroupsConjunction(Arguments, additionalChildren);
    }
}
