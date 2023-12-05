using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The requirement triggers the calculation of the Hierarchy data structure for the hierarchy of which it is a part.
/// The hierarchy of self can still be combined with <see cref="HierarchyOfReference"/> if the queried entity is a hierarchical
/// entity that is also connected to another hierarchical entity. Such situations are rather sporadic in reality.
/// The constraint accepts following arguments:
/// - specification of one or more reference names that identify the reference to the target hierarchical entity for
///   which the menu calculation should be performed; usually only one reference name makes sense, but to adapt
///   the constraint to the behavior of other similar constraints, evitaQL accepts multiple reference names for the case
///   that the same requirements apply to different references of the queried entity.
/// - optional argument of type EmptyHierarchicalEntityBehaviour enum allowing you to specify whether or not to return
///   empty hierarchical entities (e.g., those that do not have any queried entities that satisfy the current query
///   filter constraint assigned to them - either directly or transitively):
///      - <see cref="EmptyHierarchicalEntityBehaviour.LeaveEmpty"/>: empty hierarchical nodes will remain in computed data
///        structures
///      - <see cref="EmptyHierarchicalEntityBehaviour.RemoveEmpty"/>: empty hierarchical nodes are omitted from computed data
///        structures
/// - optional ordering constraint that allows you to specify an order of Hierarchy LevelInfo elements in the result
///   hierarchy data structure
/// - mandatory one or more constraints allowing you to instruct evitaDB to calculate menu components; one or all of
///   the constraints may be present:
///      <list type="bullet">
///          <item><term><see cref="HierarchyFromRoot"/></term></item>
///          <item><term><see cref="HierarchyFromNode"/></term></item>
///          <item><term><see cref="HierarchySiblings"/></term></item>
///          <item><term><see cref="HierarchyChildren"/></term></item>
///          <item><term><see cref="HierarchyParents"/></term></item>
///     </list>
/// </summary>
public class HierarchyOfSelf : AbstractRequireConstraintContainer, IRootHierarchyConstraint,
    ISeparateEntityContentRequireContainer, IExtraResultRequireConstraint
{
    public IHierarchyRequireConstraint?[] Requirements =>
        Children.Select(x => x as IHierarchyRequireConstraint).ToArray();
    
    public OrderBy? OrderBy => AdditionalChildren.OfType<OrderBy>().FirstOrDefault();
    
    public new bool Applicable => GetChildrenCount() > 0;
    
    private HierarchyOfSelf(IRequireConstraint?[] children, params IConstraint?[] additionalChildren) : base(children,
        additionalChildren)
    {
        foreach (IRequireConstraint? child in children)
        {
            Assert.IsTrue(child is IHierarchyRequireConstraint or EntityFetch,
                "Constraint HierarchyOfSelf accepts only HierarchyRequireConstraint, EntityFetch or OrderBy as inner constraints!");
        }

        foreach (IConstraint? child in additionalChildren)
        {
            Assert.IsTrue(child is OrderBy,
                "Constraint HierarchyOfSelf accepts only HierarchyRequireConstraint, EntityFetch or OrderBy as inner constraints!");
        }
    }
    
    public HierarchyOfSelf(params IHierarchyRequireConstraint?[] requirements) : base(Array.Empty<object>(), requirements)
    {
    }
    
    public HierarchyOfSelf(OrderBy? orderBy, params IHierarchyRequireConstraint?[] requirements) : base(Array.Empty<object>(), requirements, orderBy)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new HierarchyOfSelf(children, additionalChildren);
    }
}
