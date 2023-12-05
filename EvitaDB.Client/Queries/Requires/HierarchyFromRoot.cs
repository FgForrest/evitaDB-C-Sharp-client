using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `fromRoot` requirement computes the hierarchy tree starting from the "virtual" invisible top root of
/// the hierarchy, regardless of the potential use of the <see cref="HierarchyWithin"/> constraint in the filtering part of
/// the query. The scope of the calculated information can be controlled by the stopAt constraint. By default,
/// the traversal goes all the way to the bottom of the hierarchy tree unless you tell it to stop at anywhere.
/// If you need to access statistical data, use statistics constraint. Calculated data is not affected by
/// the <see cref="HierarchyWithin"/> filter constraint - the query can filter entities using <see cref="HierarchyWithin"/> from
/// category Accessories, while still allowing you to correctly compute menu at root level.
/// Please keep in mind that the full statistic calculation can be particularly expensive in the case of the fromRoot
/// requirement - it usually requires aggregation for the entire queried dataset (see more information about
/// the calculation).
/// The constraint accepts following arguments:
/// - mandatory String argument specifying the output name for the calculated data structure
/// - optional one or more constraints that allow you to define the completeness of the hierarchy entities, the scope of
///   the traversed hierarchy tree, and the statistics computed along the way; any or all of the constraints may be
///   present:
///      <list type="bullet">
///          <item><term><see cref="EntityFetch"/></term></item>
///          <item><term><see cref="HierarchyStopAt"/></term></item>
///          <item><term><see cref="HierarchyStatistics"/></term></item>
///      </list>
/// The following query lists products in category Audio and its subcategories. Along with the returned products, it also
/// requires a computed megaMenu data structure that lists the top 2 levels of the Category hierarchy tree with
/// a computed count of child categories for each menu item and an aggregated count of all filtered products that would
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
///             fromRoot(
///                 "megaMenu",
///                 entityFetch(attributeContent("code")),
///                 stopAt(level(2)),
///                 statistics(
///                     CHILDREN_COUNT,
///                     QUERIED_ENTITY_COUNT
///                 )
///             )
///         )
///     )
/// )
/// </code>
/// The calculated result for `fromRoot` is not affected by the <see cref="HierarchyWithin"/> pivot hierarchy node.
/// If the <see cref="HierarchyWithin"/> contains inner constraints <see cref="HierarchyHaving"/> or <see cref="HierarchyExcluding"/>,
/// the `fromRoot` respects them. The reason is simple: when you render a menu for the query result, you want
/// the calculated statistics to respect the rules that apply to the <see cref="HierarchyWithin"/> so that the calculated
/// number remains consistent for the end user.
/// </summary>
public class HierarchyFromRoot : AbstractRequireConstraintContainer, IHierarchyRequireConstraint
{
    private const string ConstraintName = "fromRoot";

    public string OutputName => (string) Arguments[0]!;
    public HierarchyStopAt? StopAt => (HierarchyStopAt?) Children.FirstOrDefault(x => x is HierarchyStopAt);
    public EntityFetch? EntityFetch => (EntityFetch?) Children.FirstOrDefault(x => x is EntityFetch);
    public HierarchyStatistics? Statistics => (HierarchyStatistics?) Children.FirstOrDefault(x => x is HierarchyStatistics);
    public IHierarchyOutputRequireConstraint[] OutputRequirements => Children.Where(x => x.GetType().IsAssignableFrom(typeof(IHierarchyOutputRequireConstraint))).Cast<IHierarchyOutputRequireConstraint>().ToArray();
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    
    public HierarchyFromRoot(string outputName, IRequireConstraint?[] children, params IConstraint?[] additionalChildren) : base(ConstraintName, new object[] {outputName}, children, additionalChildren)
    {
        foreach (IRequireConstraint? requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is IHierarchyOutputRequireConstraint or Requires.EntityFetch,
                "Constraint HierarchyFromRoot accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!"
            );
        }
        Assert.IsTrue(AdditionalChildren.Length == 0, "Constraint HierarchyFromRoot accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
    }
    
    public HierarchyFromRoot(string outputName, EntityFetch? entityFetch, params IHierarchyOutputRequireConstraint?[] requirements) : base(ConstraintName, new object[] {outputName}, new IRequireConstraint?[] {entityFetch}.Concat(requirements).ToArray()) 
    {
    }

    public HierarchyFromRoot(string outputName, params IHierarchyOutputRequireConstraint?[] requirements) : base(ConstraintName, new object[] {outputName}, requirements)
    {
    }
    
    public HierarchyFromRoot(string outputName) : base(ConstraintName, new object[] {outputName})
    {
    }
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new HierarchyFromRoot(OutputName, children, additionalChildren);
    }
}
