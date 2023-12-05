namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The constraint `directRelation` is a constraint that can only be used within <see cref="HierarchyWithin"/> or
/// <see cref="HierarchyWithinRoot"/> parent constraints. It simply makes no sense anywhere else because it changes the default
/// behavior of those constraints. Hierarchy constraints return all hierarchy children of the parent node or entities
/// that are transitively or directly related to them and the parent node itself. If the directRelation is used as
/// a sub-constraint, this behavior changes and only direct descendants or directly referencing entities are matched.
/// If the hierarchy constraint targets the hierarchy entity, the `directRelation` will cause only the children of
/// a direct parent node to be returned. In the case of the hierarchyWithinRoot constraint, the parent is an invisible
/// "virtual" top root - so only the top-level categories are returned.
/// <code>
/// query(
///     collection('Category'),
///     filterBy(
///         hierarchyWithinRootSelf(
///             directRelation()
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent('code')
///         )
///     )
/// )
/// </code>
/// If the hierarchy constraint targets a non-hierarchical entity that references the hierarchical one (typical example
/// is a product assigned to a category), it can only be used in the hierarchyWithin parent constraint.
/// In the case of <see cref="HierarchyWithinRoot"/>, the `directRelation` constraint makes no sense because no entity can be
/// assigned to a "virtual" top parent root.
/// So we can only list products that are directly related to a certain category. We can list products that have
/// Smartwatches category assigned:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "smartwatches"),
///             directRelation()
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// </summary>
public class HierarchyDirectRelation : AbstractFilterConstraintLeaf, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "directRelation";
    
    private HierarchyDirectRelation(params object?[] arguments) : base(ConstraintName, arguments)
    {
    }
    
    public HierarchyDirectRelation() : base(ConstraintName)
    {
    }

    public new bool Applicable => true;
}
