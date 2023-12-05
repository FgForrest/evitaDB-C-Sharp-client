using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `fromNode` requirement computes the hierarchy tree starting from the pivot node of the hierarchy, that is
/// identified by the node inner constraint. The fromNode calculates the result regardless of the potential use of
/// the <see cref="HierarchyWithin"/> constraint in the filtering part of the query. The scope of the calculated information
/// can be controlled by the <see cref="HierarchyStopAt"/> constraint. By default, the traversal goes all the way to the bottom
/// of the hierarchy tree unless you tell it to stop at anywhere. Calculated data is not affected by
/// the <see cref="HierarchyWithin"/> filter constraint - the query can filter entities using <see cref="HierarchyWithin"/> from
/// category Accessories, while still allowing you to correctly compute menu at different node defined in a `fromNode`
/// requirement. If you need to access statistical data, use statistics constraint.
/// The constraint accepts following arguments:
/// - mandatory String argument specifying the output name for the calculated data structure
/// - mandatory require constraint node that must match exactly one pivot hierarchical entity that represents the root
///   node of the traversed hierarchy subtree.
/// - optional one or more constraints that allow you to define the completeness of the hierarchy entities, the scope
///   of the traversed hierarchy tree, and the statistics computed along the way; any or all of the constraints may be
///   present:
///      <list type="bullet">
///          <item><term><see cref="EntityFetch"/></term></item>
///          <item><term><see cref="HierarchyStopAt"/></term></item>
///          <item><term><see cref="HierarchyStatistics"/></term></item>
///      </list>
/// The following query lists products in category Audio and its subcategories. Along with the products returned, it
/// also returns a computed sideMenu1 and sideMenu2 data structure that lists the flat category list for the categories
/// Portables and Laptops with a computed count of child categories for each menu item and an aggregated count of all
/// products that would fall into the given category.
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
///             fromNode(
///                 "sideMenu1",
///                 node(
///                     filterBy(
///                         attributeEquals("code", "portables")
///                     )
///                 ),
///                 entityFetch(attributeContent("code")),
///                 stopAt(distance(1)),
///                 statistics(
///                     CHILDREN_COUNT,
///                     QUERIED_ENTITY_COUNT
///                 )
///             ),
///             fromNode(
///                 "sideMenu2",
///                 node(
///                     filterBy(
///                         attributeEquals("code", "laptops")
///                     )
///                 ),
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
/// The calculated result for `fromNode` is not affected by the <see cref="HierarchyWithin"/> pivot hierarchy node.
/// If the <see cref="HierarchyWithin"/> contains inner constraints <see cref="HierarchyHaving"/> or <see cref="HierarchyExcluding"/>,
/// the `fromNode` respects them. The reason is simple: when you render a menu for the query result, you want
/// the calculated statistics to respect the rules that apply to the hierarchyWithin so that the calculated number
/// remains consistent for the end user.
/// </summary>
public class HierarchyFromNode : AbstractRequireConstraintContainer, IHierarchyRequireConstraint
{
    private const string ConstraintName = "fromNode";
    public string OutputName => Arguments[0]?.ToString()!;

    public HierarchyNode FromNode => (HierarchyNode) (Children.FirstOrDefault(x => x is HierarchyNode) ??
                                                      throw new InvalidOperationException(
                                                          "The HierarchyNode inner constraint unexpectedly not found!"));

    public HierarchyStopAt? StopAt => (HierarchyStopAt?) Children.FirstOrDefault(x => x is HierarchyStopAt);
    public EntityFetch? EntityFetch => (EntityFetch?) Children.FirstOrDefault(x => x is EntityFetch);

    public HierarchyStatistics? Statistics =>
        (HierarchyStatistics?) Children.FirstOrDefault(x => x is HierarchyStatistics);

    public IHierarchyOutputRequireConstraint[] OutputRequirements => Children
        .Where(x => x.GetType().IsAssignableFrom(typeof(IHierarchyOutputRequireConstraint)))
        .Cast<IHierarchyOutputRequireConstraint>().ToArray();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1 && Children.Length >= 1;

    private HierarchyFromNode(string outputName, IRequireConstraint?[] children, params IConstraint?[] additionalChildren)
        : base(ConstraintName, new object[] {outputName}, children, additionalChildren)
    {
        foreach (IRequireConstraint? requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is HierarchyNode or IHierarchyOutputRequireConstraint or Requires.EntityFetch,
                "Constraint HierarchyFromNode accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
        }

        Assert.IsTrue(
            additionalChildren.Length == 0,
            "Constraint HierarchyFromNode accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!"
        );
    }

    public HierarchyFromNode(string outputName, HierarchyNode node, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[] requirements)
        : base(ConstraintName, new object[] {outputName},
            new IRequireConstraint?[] {node, entityFetch}.Concat(requirements).ToArray())
    {
    }

    public HierarchyFromNode(string outputName, HierarchyNode fromNode) : base(ConstraintName,
        new object[] {outputName}, fromNode)
    {
    }

    public HierarchyFromNode(string outputName, HierarchyNode fromNode,
        params IHierarchyOutputRequireConstraint[] requirements) : base(ConstraintName, new object[] {outputName},
        new IRequireConstraint[] {fromNode}.Concat(requirements).ToArray())
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new HierarchyFromNode(OutputName, children, additionalChildren);
    }
}
