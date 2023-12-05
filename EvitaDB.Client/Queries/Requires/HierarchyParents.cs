using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The parents requirement computes the hierarchy tree starting at the same hierarchy node that is targeted by
/// the filtering part of the same query using the hierarchyWithin constraint towards the root of the hierarchy.
/// The scope of the calculated information can be controlled by the stopAt constraint. By default, the traversal goes
/// all the way to the top of the hierarchy tree unless you tell it to stop at anywhere. If you need to access
/// statistical data, use the statistics constraint.
/// The constraint accepts following arguments:
/// - mandatory String argument specifying the output name for the calculated data structure
/// - optional one or more constraints that allow you to define the completeness of the hierarchy entities, the scope
///   of the traversed hierarchy tree, and the statistics computed along the way; any or all of the constraints may be
///   present:
///      <list type="bullet">
///          <item><term><see cref="HierarchySiblings"/></term></item>
///          <item><term><see cref="EntityFetch"/></term></item>
///          <item><term><see cref="HierarchyStopAt"/></term></item>
///          <item><term><see cref="HierarchyStatistics"/></term></item>
///      </list>
/// The following query lists products in the category Audio and its subcategories. Along with the products returned,
/// it also returns a computed parentAxis data structure that lists all the parent nodes of the currently focused
/// category True wireless with a computed count of child categories for each menu item and an aggregated count of all
/// products that would fall into the given category.
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "true-wireless")
///         )
///     ),
///     require(
///         hierarchyOfReference(
///             "categories",
///             parents(
///                 "parentAxis",
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
/// The calculated result for parents is connected with the <see cref="HierarchyWithin"/> pivot hierarchy node.
/// If the <see cref="HierarchyWithin"/> contains inner constraints <see cref="HierarchyHaving"/> or <see cref="HierarchyExcluding"/>,
/// the parents will respect them as well during child nodes / queried entities statistics calculation. The reason is
/// simple: when you render a menu for the query result, you want the calculated statistics to respect the rules that
/// apply to the <see cref="HierarchyWithin"/> so that the calculated number remains consistent for the end user.
/// </summary>
public class HierarchyParents : AbstractRequireConstraintContainer, IHierarchyRequireConstraint
{
    private const string ConstraintName = "parents";
    public string OutputName => (string) Arguments[0]!;
    public HierarchyStopAt? StopAt => (HierarchyStopAt?) Children.FirstOrDefault(x => x is HierarchyStopAt);
    public HierarchySiblings? Siblings => (HierarchySiblings?) Children.FirstOrDefault(x => x is HierarchySiblings);
    public EntityFetch? EntityFetch => (EntityFetch?) Children.FirstOrDefault(x => x is EntityFetch);

    public HierarchyStatistics? Statistics =>
        (HierarchyStatistics?) Children.FirstOrDefault(x => x is HierarchyStatistics);

    public IHierarchyOutputRequireConstraint[] OutputRequirements =>
        Children.OfType<IHierarchyOutputRequireConstraint>().ToArray();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;

    private HierarchyParents(string outputName, IRequireConstraint?[] children) : base(ConstraintName,
        new object[] {outputName}, children)
    {
        foreach (IRequireConstraint? requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is IHierarchyOutputRequireConstraint or HierarchySiblings or Requires.EntityFetch,
                "Constraint HierarchyParents accepts only HierarchyStopAt, HierarchyStatistics, HierarchySiblings and EntityFetch as inner constraints!");
        }
    }

    public HierarchyParents(string outputName, EntityFetch? entityFetch, params IHierarchyOutputRequireConstraint[] requirements)
        : base(ConstraintName, new object[]{outputName}, new IRequireConstraint?[]{entityFetch}.Concat(requirements).ToArray())
    {
    }
    
    public HierarchyParents(string outputName, EntityFetch? entityFetch, HierarchySiblings? hierarchySiblings, params IHierarchyOutputRequireConstraint[] requirements)
        : base(ConstraintName, new object[]{outputName}, new IRequireConstraint?[]{entityFetch, hierarchySiblings}.Concat(requirements).ToArray())
    {
    }
    
    public HierarchyParents(string outputName, params IHierarchyOutputRequireConstraint?[] requirements)
        : base(ConstraintName, new object?[]{outputName}, requirements)
    {
    }
    
    public HierarchyParents(string outputName, HierarchySiblings? siblings, params IHierarchyOutputRequireConstraint?[] requirements)
        : base(ConstraintName, new object[]{outputName}, new IRequireConstraint?[]{siblings}.Concat(requirements).ToArray())
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        Assert.IsTrue(additionalChildren.Length == 0,
            "Inner constraints of different type than `require` are not expected.");
        return new HierarchyParents(OutputName, children);
    }
}
