using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The distance constraint can only be used within the <see cref="HierarchyStopAt"/> container and limits the hierarchy
/// traversal to stop when the number of levels traversed reaches the specified constant. The distance is always relative
/// to the pivot node (the node where the hierarchy traversal starts) and is the same whether we are traversing
/// the hierarchy top-down or bottom-up. The distance between any two nodes in the hierarchy can be calculated as
/// `abs(level(nodeA) - level(nodeB))`.
/// The constraint accepts single integer argument `distance`, which defines a maximum relative distance from the pivot
/// node that can be traversed; the pivot node itself is at distance zero, its direct child or direct parent is
/// at distance one, each additional step adds a one to the distance.
/// See the following figure when the pivot node is Audio:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "audio")
///         )
///     ),
///     require(
///         hierarchyOfReference(
///             "categories",
///             children(
///                 "subcategories",
///                 entityFetch(attributeContent("code")),
///                 stopAt(distance(1))
///             )
///         )
///     )
/// )
/// </code>
/// The following query lists products in category Audio and its subcategories. Along with the products returned, it
/// also returns a computed subcategories data structure that lists the flat category list the currently focused category
/// Audio.
/// </summary>
public class HierarchyDistance : AbstractRequireConstraintLeaf, IHierarchyStopAtRequireConstraint
{
    private const string ConstraintName = "distance";
    
    public int Distance => (int) Arguments[0]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;

    private HierarchyDistance(params object?[] arguments) : base(ConstraintName, arguments)
    {
    }
    
    public HierarchyDistance(int distance) : base(ConstraintName, distance)
    {
        Assert.IsTrue(distance > 0, () => new EvitaInvalidUsageException("Distance must be greater than zero."));
    }
}
