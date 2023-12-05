using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The level constraint can only be used within the stopAt container and limits the hierarchy traversal to stop when
/// the actual level of the traversed node is equal to a specified constant. The "virtual" top invisible node has level
/// zero, the top nodes (nodes with NULL parent) have level one, their children have level two, and so on.
/// See the following figure:
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
///             fromRoot(
///                 "megaMenu",
///                 entityFetch(attributeContent("code")),
///                 stopAt(level(2))
///             )
///         )
///     )
/// )
/// </code>
/// The query lists products in Audio category and its subcategories. Along with the products returned, it
/// also returns a computed megaMenu data structure that lists top two levels of the entire hierarchy.
/// </summary>
public class HierarchyLevel : AbstractRequireConstraintLeaf, IHierarchyStopAtRequireConstraint
{
    private const string ConstraintName = "level";
    
    public int Level => (int) Arguments[0]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    
    private HierarchyLevel(params object?[] arguments) : base(ConstraintName, arguments)
    {
    }

    public HierarchyLevel(int level) : base(ConstraintName, level)
    {
        Assert.IsTrue(level > 0, () => new EvitaInvalidUsageException("Level must be greater than zero. Level 1 represents root node."));
    }
}
