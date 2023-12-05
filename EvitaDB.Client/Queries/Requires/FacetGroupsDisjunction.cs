using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// This `facetGroupsDisjunction` require constraint allows specifying facet relation among different facet groups of certain
/// primary ids. First mandatory argument specifies entity type of the facet group, secondary argument allows to define one
/// more facet group ids that should be considered disjunctive.
/// This require constraint changes default behaviour stating that facets between two different facet groups are combined by
/// AND relation and changes it to the disjunction relation instead.
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
///       facetGroupsDisjunction("parameterType", 1, 2)
///    )
/// )
/// </code>
/// This statement means, that facets in `parameterType` facet groups `1`, `2` will be joined with the rest of the query by
/// boolean OR relation when selected.
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
/// When user selects facets: blue (11), large (22), new products (31) - the default meaning would be: get all entities that
/// have facet blue as well as facet large and action products tag (AND). If require `facetGroupsDisjunction('tag', 3)`
/// is passed in the query, filtering condition will be composed as: (`blue(11)` AND `large(22)`) OR `new products(31)`
/// </summary>
public class FacetGroupsDisjunction : AbstractRequireConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;

    public FilterBy? FacetGroups => AdditionalChildren.OfType<FilterBy>().FirstOrDefault();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 0;

    private FacetGroupsDisjunction(object?[] arguments, params IConstraint?[] additionalChildren) : base(arguments,
        NoChildren, additionalChildren)
    {
        foreach (IConstraint? child in additionalChildren)
        {
            Assert.IsPremiseValid(child is FilterBy,
                "Only FilterBy constraints are allowed in FacetGroupsDisjunction.");
        }
    }

    public FacetGroupsDisjunction(string referenceName, FilterBy? filterBy) : base(new object[] {referenceName},
        NoChildren, filterBy)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        Assert.IsPremiseValid(children.Length == 0, "Children must be empty.");
        return new FacetGroupsDisjunction(Arguments, additionalChildren);
    }
}
