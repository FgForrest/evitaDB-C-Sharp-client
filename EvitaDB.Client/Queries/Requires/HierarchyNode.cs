using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The node filtering container is an alternative to the <see cref="HierarchyDistance"/> and <see cref="HierarchyLevel"/>
/// termination constraints, which is much more dynamic and can produce hierarchy trees of non-uniform depth. Because
/// the filtering constraint can be satisfied by nodes of widely varying depths, traversal can be highly dynamic.
/// Constraint children define a criterion that determines the point in a hierarchical structure where the traversal
/// should stop. The traversal stops at the first node that satisfies the filter condition specified in this container.
/// The situations where you'd need this dynamic behavior are few and far between. Unfortunately, we do not have
/// a meaningful example of this in the demo dataset, so our example query will be slightly off. But for the sake of
/// demonstration, let's list the entire Accessories hierarchy, but stop traversing at the nodes whose code starts with
/// the letter `w`.
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "accessories")
///         )
///     ),
///     require(
///         hierarchyOfReference(
///             "categories",
///             children(
///                 "subMenu",
///                 entityFetch(attributeContent("code")),
///                 stopAt(
///                     node(
///                         filterBy(
///                             attributeStartsWith("code", "w")
///                         )
///                     )
///                 )
///             )
///         )
///     )
/// )
/// </code>
/// </summary>
public class HierarchyNode : AbstractRequireConstraintContainer, IHierarchyStopAtRequireConstraint
{
    private const string ConstraintName = "node";
    
    public FilterBy FilterBy => GetAdditionalChild(typeof(FilterBy)) as FilterBy ?? throw new InvalidOperationException("Hierarchy node expects FilterBy as its single inner constraint!");
    public new bool Applicable => AdditionalChildren.Length == 1;
    
    public HierarchyNode(FilterBy filterBy) : base(ConstraintName, Array.Empty<object>(), Array.Empty<IRequireConstraint>(), filterBy)
    {
    }
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint?[] additionalChildren)
    {
        Assert.IsTrue(children.Length == 0, "Inner constraints of different type than FilterBy are not expected.");
        Assert.IsTrue(additionalChildren.Length == 1, "HierarchyNode expect FilterBy inner constraint!");
        foreach (IConstraint? constraint in additionalChildren)
        {
            Assert.IsTrue(constraint is FilterBy, "Constraint HierarchyNode accepts only FilterBy as inner constraint!");
        }
        return new HierarchyNode((FilterBy) additionalChildren[0]!);
    }
}
