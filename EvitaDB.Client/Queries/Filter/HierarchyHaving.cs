using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The constraint `having` is a constraint that can only be used within <see cref="HierarchyWithin"/> or
/// <see cref="HierarchyWithinRoot"/> parent constraints. It simply makes no sense anywhere else because it changes the default
/// behavior of those constraints. Hierarchy constraints return all hierarchy children of the parent node or entities
/// that are transitively or directly related to them, and the parent node itself.
/// The having constraint allows you to set a constraint that must be fulfilled by all categories in the category scope
/// in order to be accepted by hierarchy within filter. This constraint is especially useful if you want to conditionally
/// display certain parts of the tree. Imagine you have a category Christmas Sale that should only be available during
/// a certain period of the year, or a category B2B Partners that should only be accessible to a certain role of users.
/// All of these scenarios can take advantage of the having constraint (but there are other approaches to solving
/// the above use cases).
/// The constraint accepts following arguments:
/// - one or more mandatory constraints that must be satisfied by all returned hierarchy nodes and that mark the visible
///   part of the tree, the implicit relation between constraints is logical conjunction (boolean AND)
/// When the hierarchy constraint targets the hierarchy entity, the children that don't satisfy the inner constraints
/// (and their children, whether they satisfy them or not) are excluded from the result.
/// For demonstration purposes, let's list all categories within the Accessories category, but only those that are valid
/// at 01:00 AM on October 1, 2023.
/// <code>
/// query(
///     collection('Category'),
///     filterBy(
///         hierarchyWithinSelf(
///             attributeEquals('code', 'accessories'),
///             having(
///                 or(
///                     attributeIsNull('validity'),
///                     attributeInRange('validity', 2023-10-01T01:00:00-01:00)
///                 )
///             )
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent('code')
///         )
///     )
/// )
/// </code>
/// Because the category Christmas electronics has its validity set to be valid only between December 1st and December
/// 24th, it will be omitted from the result. If it had subcategories, they would also be omitted (even if they had no
/// validity restrictions).
/// If the hierarchy constraint targets a non-hierarchical entity that references the hierarchical one (typical example
/// is a product assigned to a category), the having constraint is evaluated against the hierarchical entity (category),
/// but affects the queried non-hierarchical entities (products). It excludes all products referencing categories that
/// don't satisfy the having inner constraints.
/// Let's use again our example with Christmas electronics that is valid only between 1st and 24th December. To list all
/// products available at 01:00 AM on October 1, 2023, issue a following query:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "accessories"),
///             having(
///                 or(
///                     attributeIsNull("validity"),
///                     attributeInRange("validity", 2023-10-01T01:00:00-01:00)
///                 )
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
/// You can see that Christmas products like Retlux Blue Christmas lightning, Retlux Warm white Christmas lightning or
/// Emos Candlestick are not present in the listing.
/// The lookup stops at the first node that doesn't satisfy the constraint!
/// The hierarchical query traverses from the root nodes to the leaf nodes. For each of the nodes, the engine checks
/// whether the having constraint is still valid, and if not, it excludes that hierarchy node and all of its child nodes
/// (entire subtree).
/// What if the product is linked to two categories - one that meets the constraint and one that does not?
/// In the situation where the single product, let's say Garmin Vivosmart 5, is in both the excluded category Christmas
/// Electronics and the included category Smartwatches, it will remain in the query result because there is at least one
/// product reference that is part of the visible part of the tree.
/// </summary>
public class HierarchyHaving : AbstractFilterConstraintContainer, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "having";
    
    public HierarchyHaving(params IFilterConstraint?[] children) : base(ConstraintName, NoArguments, children)
    {
    }
    
    public IFilterConstraint[] Filtering => Children;
    
    public new bool Necessary => Children.Length > 0;
    
    public new  bool Applicable => Children.Length > 0;
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        Assert.IsTrue(
            additionalChildren.Length == 0,
            "Constraint HierarchyHaving doesn't accept other than filtering constraints!"
        );
        return new HierarchyHaving(children);
    }
}
