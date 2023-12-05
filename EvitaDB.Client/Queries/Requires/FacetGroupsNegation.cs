using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `facetGroupsNegation` changes the behavior of the facet option in all facet groups specified in the filterBy
/// constraint. Instead of returning only those items that have a reference to that particular faceted entity, the query
/// result will return only those items that don't have a reference to it.
/// Example:
/// <code>
/// query(
///     collection("Product"),
///     require(
///         facetSummaryOfReference(
///             "parameterValues",
///             IMPACT,
///             filterBy(attributeContains("code", "4")),
///             filterGroupBy(attributeInSet("code", "ram-memory", "rom-memory")),
///             entityFetch(attributeContent("code")),
///             entityGroupFetch(attributeContent("code"))
///         ),
///         facetGroupsNegation(
///             "parameterValues",
///             filterBy(
///               attributeInSet("code", "ram-memory")
///             )
///         )
///     )
/// )
/// </code>
/// The predicted results in the negated groups are far greater than the numbers produced by the default behavior.
/// Selecting any option in the RAM facet group predicts returning thousands of results, while the ROM facet group with
/// default behavior predicts only a dozen of them.
/// </summary>
public class FacetGroupsNegation : AbstractRequireConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;

    public FilterBy? FacetGroups => AdditionalChildren.OfType<FilterBy>().FirstOrDefault();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 0;

    private FacetGroupsNegation(object?[] arguments, params IConstraint?[] additionalChildren) : base(arguments,
        NoChildren, additionalChildren)
    {
        foreach (IConstraint? child in additionalChildren)
        {
            Assert.IsPremiseValid(child is FilterBy,
                "Only FilterBy constraints are allowed in FacetGroupsNegation.");
        }
    }

    public FacetGroupsNegation(string referenceName, FilterBy? filterBy) : base(new object[] {referenceName},
        NoChildren, filterBy)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        Assert.IsPremiseValid(children.Length == 0, "Children must be empty.");
        return new FacetGroupsNegation(Arguments, additionalChildren);
    }
}
