namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The constraint `excludingRoot` is a constraint that can only be used within <see cref="HierarchyWithin"/> or
/// <see cref="HierarchyWithinRoot"/> parent constraints. It simply makes no sense anywhere else because it changes the default
/// behavior of those constraints. Hierarchy constraints return all hierarchy children of the parent node or entities
/// that are transitively or directly related to them and the parent node itself. When the excludingRoot is used as
/// a sub-constraint, this behavior changes and the parent node itself or the entities directly related to that parent
/// node are be excluded from the result.
/// If the hierarchy constraint targets the hierarchy entity, the `excludingRoot` will omit the requested parent node
/// from the result. In the case of the <see cref="HierarchyWithinRoot"/> constraint, the parent is an invisible "virtual" top
/// root, and this constraint makes no sense.
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "accessories"),
///             excludingRoot()
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// If the hierarchy constraint targets a non-hierarchical entity that references the hierarchical one (typical example
/// is a product assigned to a category), the `excludingRoot` constraint can only be used in the <see cref="HierarchyWithin"/>
/// parent constraint.
/// In the case of <see cref="HierarchyWithinRoot"/>, the `excludingRoot` constraint makes no sense because no entity can be
/// assigned to a "virtual" top parent root.
/// Because we learned that Accessories category has no directly assigned products, the `excludingRoot` constraint
/// presence would not affect the query result. Therefore, we choose Keyboard category for our example. When we list all
/// products in Keyboard category using <see cref="HierarchyWithin"/> constraint, we obtain 20 items. When the `excludingRoot`
/// constraint is used:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "keyboards"),
///             excludingRoot()
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// ... we get only 4 items, which means that 16 were assigned directly to Keyboards category and only 4 of them were
/// assigned to Exotic keyboards.
/// </summary>
public class HierarchyExcludingRoot : AbstractFilterConstraintLeaf, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "excludingRoot";
    
    private HierarchyExcludingRoot(params object?[] arguments) : base(ConstraintName, arguments)
    {
    }
    
    public HierarchyExcludingRoot() : base(ConstraintName)
    {
    }
    
    public new bool Applicable => true;
}
