using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The constraint `excluding` is a constraint that can only be used within <see cref="HierarchyWithin"/> or
/// <see cref="HierarchyWithinRoot"/> parent constraints. It simply makes no sense anywhere else because it changes the default
/// behavior of those constraints. Hierarchy constraints return all hierarchy children of the parent node or entities
/// that are transitively or directly related to them, and the parent node itself.
/// The excluding constraint allows you to exclude one or more subtrees from the scope of the filter. This constraint is
/// the exact opposite of the having constraint. If the constraint is true for a hierarchy entity, it and all of its
/// children are excluded from the query. The excluding constraint is the same as declaring `having(not(expression))`,
/// but for the sake of readability it has its own constraint.
/// The constraint accepts following arguments:
/// - one or more mandatory constraints that must be satisfied by all returned hierarchy nodes and that mark the visible
///   part of the tree, the implicit relation between constraints is logical conjunction (boolean AND)
/// When the hierarchy constraint targets the hierarchy entity, the children that satisfy the inner constraints (and
/// their children, whether they satisfy them or not) are excluded from the result.
/// For demonstration purposes, let's list all categories within the Accessories category, but exclude exactly
/// the Wireless headphones subcategory.
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "accessories"),
///             excluding(
///                 attributeEquals("code", "wireless-headphones")
///             )
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// The category Wireless Headphones and all its subcategories will not be shown in the results list.
/// If the hierarchy constraint targets a non-hierarchical entity that references the hierarchical one (typical example
/// is a product assigned to a category), the excluding constraint is evaluated against the hierarchical entity
/// (category), but affects the queried non-hierarchical entities (products). It excludes all products referencing
/// categories that satisfy the excluding inner constraints.
/// Let's go back to our example query that excludes the Wireless Headphones category subtree. To list all products
/// available in the Accessories category except those related to the Wireless Headphones category or its subcategories,
/// issue the following query:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "accessories"),
///             excluding(
///                 attributeEquals("code", "wireless-headphones")
///             )
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// You can see that wireless headphone products like Huawei FreeBuds 4, Jabra Elite 3 or Adidas FWD-02 Sport are not
/// present in the listing.
/// When the product is assigned to two categories - one excluded and one part of the visible category tree, the product
/// remains in the result. See the example.
/// The lookup stops at the first node that satisfies the constraint!
/// The hierarchical query traverses from the root nodes to the leaf nodes. For each of the nodes, the engine checks
/// whether the excluding constraint is satisfied valid, and if so, it excludes that hierarchy node and all of its child
/// nodes (entire subtree).
/// </summary>
public class HierarchyExcluding : AbstractFilterConstraintContainer, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "excluding";
    
    public HierarchyExcluding(params IFilterConstraint?[] filtering) : base(ConstraintName, NoArguments, filtering)
    {
    }
    
    public IFilterConstraint[] Filtering => Children;
    
    public override bool Applicable => Children.Length > 0;
    public new bool Necessary => Children.Length > 0;
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        Assert.IsTrue(
            additionalChildren.Length == 0,
            "Constraint HierarchyExcluding doesn't accept other than filtering constraints!"
        );
        return new HierarchyExcluding(children);
    }
}
