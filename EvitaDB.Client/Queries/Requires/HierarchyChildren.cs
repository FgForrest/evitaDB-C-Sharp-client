using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The children requirement computes the hierarchy tree starting at the same hierarchy node that is targeted by
/// the filtering part of the same query using the <see cref="HierarchyWithin"/> or <see cref="HierarchyWithinRoot"/> constraints.
/// The scope of the calculated information can be controlled by the stopAt constraint. By default, the traversal goes
/// all the way to the bottom of the hierarchy tree unless you tell it to stop at anywhere. If you need to access
/// statistical data, use the statistics constraint.
/// The constraint accepts following arguments:
/// - mandatory String argument specifying the output name for the calculated data structure
/// - optional one or more constraints that allow you to define the completeness of the hierarchy entities, the scope of
///   the traversed hierarchy tree, and the statistics computed along the way; any or all of the constraints may be
///   present:
///   <list type="bullet">
///       <item><term><see cref="EntityFetch"/></term></item>
///       <item><term><see cref="HierarchyStopAt"/></term></item>
///       <item><term><see cref="HierarchyStatistics"/></term></item>
///   </list>
/// The following query lists products in category Audio and its subcategories. Along with the products returned, it also
/// returns a computed subcategories data structure that lists the flat category list the currently focused category
/// Audio with a computed count of child categories for each menu item and an aggregated count of all products that would
/// fall into the given category.
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
///                 stopAt(distance(1)),
///                 statistics(
///                     CHILDREN_COUNT,
///                     QUERIED_ENTITY_COUNT
///                 )
///             )
///         )
///     )
/// )
/// </code>
/// The calculated result for children is connected with the <see cref="HierarchyWithin"/> pivot hierarchy node (or
/// the "virtual" invisible top root referred to by the hierarchyWithinRoot constraint). If the <see cref="HierarchyWithin"/>
/// contains inner constraints <see cref="HierarchyHaving"/> or <see cref="HierarchyExcluding"/>, the children will respect them as
/// well. The reason is simple: when you render a menu for the query result, you want the calculated statistics to
/// respect the rules that apply to the hierarchyWithin so that the calculated number remains consistent for the end
/// user.
/// </summary>
public class HierarchyChildren : AbstractRequireConstraintContainer, IHierarchyRequireConstraint
{
    private const string ConstraintName = "children";

    public string OutputName => (string) Arguments[0]!;
    public HierarchyStopAt? StopAt => Children.FirstOrDefault(x => x is HierarchyStopAt) as HierarchyStopAt;
    public EntityFetch? EntityFetch => Children.FirstOrDefault(x => x is EntityFetch) as EntityFetch;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    
    private HierarchyChildren(string outputName, IRequireConstraint?[] children, params IConstraint?[] additionalChildren)
        : base(ConstraintName, new object[] {outputName}, children, additionalChildren)
    {
        foreach (IRequireConstraint? requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is IHierarchyOutputRequireConstraint or Requires.EntityFetch,
                "Constraint HierarchyChildren accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!"
            );
        }
        Assert.IsTrue(AdditionalChildren.Length == 0, "Constraint HierarchyChildren accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
    }

    public HierarchyChildren(string outputName, EntityFetch? entityFetch, params IHierarchyOutputRequireConstraint[] requirements)
        : base(ConstraintName, new object[]{outputName}, new IRequireConstraint?[]{entityFetch}.Concat(requirements).ToArray())
    {
    }
    
    public HierarchyChildren(string outputName, params IHierarchyOutputRequireConstraint[] requirements) : base(ConstraintName, new object[]{outputName}, requirements)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new HierarchyChildren(OutputName, children, additionalChildren);
    }
}
