using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The siblings requirement computes the hierarchy tree starting at the same hierarchy node that is targeted by
/// the filtering part of the same query using the hierarchyWithin. It lists all sibling nodes to the node that is
/// requested by hierarchyWithin constraint (that's why the siblings has no sense with <see cref="HierarchyWithinRoot"/>
/// constraint - "virtual" top level node cannot have any siblings). Siblings will produce a flat list of siblings unless
/// the <see cref="HierarchyStopAt"/> constraint is used as an inner constraint. The <see cref="HierarchyStopAt"/> constraint
/// triggers a top-down hierarchy traversal from each of the sibling nodes until the <see cref="HierarchyStopAt"/> is
/// satisfied. If you need to access statistical data, use the statistics constraint.
/// The constraint accepts following arguments:
/// - mandatory String argument specifying the output name for the calculated data structure
/// - optional one or more constraints that allow you to define the completeness of the hierarchy entities, the scope
///   of the traversed hierarchy tree, and the statistics computed along the way; any or all of the constraints may
///   be present:
///      <list type="bullet">
///          <item><term><see cref="EntityFetch"/></term></item>
///          <item><term><see cref="HierarchyStopAt"/></term></item>
///          <item><term><see cref="HierarchyStatistics"/></term></item>
///     </list>
/// The following query lists products in category Audio and its subcategories. Along with the products returned, it also
/// returns a computed audioSiblings data structure that lists the flat category list the currently focused category
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
///             siblings(
///                 "audioSiblings",
///                 entityFetch(attributeContent("code")),
///                 statistics(
///                     CHILDREN_COUNT,
///                     QUERIED_ENTITY_COUNT
///                 )
///             )
///         )
///     )
/// )
/// </code>
/// The calculated result for siblings is connected with the <see cref="HierarchyWithin"/> pivot hierarchy node. If
/// the <see cref="HierarchyWithin"/> contains inner constraints <see cref="HierarchyHaving"/> or <see cref="HierarchyExcluding"/>,
/// the children will respect them as well. The reason is simple: when you render a menu for the query result, you want
/// the calculated statistics to respect the rules that apply to the hierarchyWithin so that the calculated number
/// remains consistent for the end user.
/// 
/// Different siblings syntax when used within parents parent constraint
/// 
/// The siblings constraint can be used separately as a child of <see cref="HierarchyOfSelf"/> or <see cref="HierarchyOfReference"/>,
/// or it can be used as a child constraint of <see cref="HierarchyParents"/>. In such a case, the siblings constraint lacks
/// the first string argument that defines the name for the output data structure. The reason is that this name is
/// already defined on the enclosing parents constraint, and the siblings constraint simply extends the data available
/// in its data structure.
/// </summary>
public class HierarchySiblings : AbstractRequireConstraintContainer, IHierarchyRequireConstraint
{
    private const string ConstraintName = "siblings";
    
    public string? OutputName => Arguments.Length > 0 ? (string?) Arguments[0] : null;
    public HierarchyStopAt? StopAt => Children.FirstOrDefault(x => x is HierarchyStopAt) as HierarchyStopAt;
    public EntityFetch? EntityFetch => Children.FirstOrDefault(x => x is EntityFetch) as EntityFetch;
    public HierarchyStatistics? Statistics => Children.FirstOrDefault(x => x is HierarchyStatistics) as HierarchyStatistics;

    public IHierarchyOutputRequireConstraint[] OutputRequirements =>
        Children.OfType<IHierarchyOutputRequireConstraint>().ToArray();
    
    public new bool Applicable => true;
    
    private HierarchySiblings(string? outputName, IRequireConstraint?[] children, params IConstraint?[] additionalChildren) : base(ConstraintName, new object?[] {outputName}, children, additionalChildren)
    {
        foreach (IRequireConstraint? requireConstraint in children)
        {
            Assert.IsTrue(requireConstraint is IHierarchyOutputRequireConstraint or Requires.EntityFetch, "Constraint HierarchySiblings accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
        }
        Assert.IsTrue(additionalChildren.Length == 0, "Constraint HierarchySiblings accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
    }
    
    public HierarchySiblings(string? outputName, EntityFetch? entityFetch, params IHierarchyOutputRequireConstraint?[] requirements) 
        : base(ConstraintName, outputName is null 
            ? NoArguments : 
            new object?[] {outputName}, new IRequireConstraint?[] {entityFetch}.Concat(requirements).ToArray()) 
    {
    }
    
    public HierarchySiblings(string? outputName, params IHierarchyOutputRequireConstraint?[] requirements)
        : base(ConstraintName, outputName is null ? NoArguments :  new object?[] {outputName}, requirements)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new HierarchySiblings(OutputName, children, additionalChildren);
    }

    
}
