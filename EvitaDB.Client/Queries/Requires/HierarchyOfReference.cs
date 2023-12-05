using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The requirement triggers the calculation of the Hierarchy data structure for the hierarchies of the referenced entity
/// type.
/// The hierarchy of reference can still be combined with <see cref="HierarchyOfSelf"/> if the queried entity is a hierarchical
/// entity that is also connected to another hierarchical entity. Such situations are rather sporadic in reality.
/// The `hierarchyOfReference` can be repeated multiple times in a single query if you need different calculation
/// settings for different reference types.
/// The constraint accepts following arguments:
/// - specification of one or more reference names that identify the reference to the target hierarchical entity for
///   which the menu calculation should be performed; usually only one reference name makes sense, but to adapt
///   the constraint to the behavior of other similar constraints, evitaQL accepts multiple reference names for the case
///   that the same requirements apply to different references of the queried entity.
/// - optional argument of type EmptyHierarchicalEntityBehaviour enum allowing you to specify whether or not to return
///   empty hierarchical entities (e.g., those that do not have any queried entities that satisfy the current query
///   filter constraint assigned to them - either directly or transitively):
///      - <see cref="Requires.EmptyHierarchicalEntityBehaviour.LeaveEmpty"/>: empty hierarchical nodes will remain in computed data
///        structures
///      - <see cref="Requires.EmptyHierarchicalEntityBehaviour.RemoveEmpty"/>: empty hierarchical nodes are omitted from computed data
///        structures (default behavior)
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
///      </list>
/// </summary>
public class HierarchyOfReference : AbstractRequireConstraintContainer, IRootHierarchyConstraint,
    ISeparateEntityContentRequireContainer, IExtraResultRequireConstraint
{
    public string ReferenceName
    {
        get
        {
            string[] referenceNames = ReferenceNames;
            Assert.IsTrue(referenceNames.Length == 1, "There are multiple reference names, cannot get only one.");
            return referenceNames[0];
        }
    }

    public string[] ReferenceNames => Arguments.OfType<string>().ToArray();

    public EmptyHierarchicalEntityBehaviour EmptyHierarchicalEntityBehaviour =>
        (EmptyHierarchicalEntityBehaviour) (Arguments.FirstOrDefault(x => x is EmptyHierarchicalEntityBehaviour) ??
                                            throw new EvitaInternalError(
                                                "EmptyHierarchicalEntityBehaviour is a mandatory argument!"));

    public IHierarchyRequireConstraint?[] Requirements =>
        Children.Select(x => x as IHierarchyRequireConstraint).ToArray();

    public OrderBy? OrderBy => AdditionalChildren.OfType<OrderBy>().FirstOrDefault();

    public new bool Applicable => Arguments.Length > 0 && GetChildrenCount() > 0;
    
    private HierarchyOfReference(object?[] arguments, IRequireConstraint?[] children,
        params IConstraint?[] additionalChildren)
        : base(arguments, children, additionalChildren)
    {
        foreach (IRequireConstraint? child in children)
        {
            Assert.IsTrue(child is IHierarchyRequireConstraint or EntityFetch,
                "Constraint HierarchyOfReference accepts only HierarchyRequireConstraint, EntityFetch or OrderBy as inner constraints!");
        }

        foreach (IConstraint? child in additionalChildren)
        {
            Assert.IsTrue(child is OrderBy,
                "Constraint HierarchyOfReference accepts only HierarchyRequireConstraint, EntityFetch or OrderBy as inner constraints!");
        }
    }

    public HierarchyOfReference(string referenceName, EmptyHierarchicalEntityBehaviour emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint?[] requirements) : base(
        new object[] {referenceName, emptyHierarchicalEntityBehaviour}, requirements)
    {
    }

    public HierarchyOfReference(string[] referenceNames,
        EmptyHierarchicalEntityBehaviour emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint?[] requirements) : base(
        referenceNames.Select(x => x as object).ToArray().Concat(new object[] {emptyHierarchicalEntityBehaviour})
            .ToArray(), requirements)
    {
    }

    public HierarchyOfReference(string referenceName,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint?[] requirements) : base(
        new object?[] {referenceName, emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty},
        requirements)
    {
    }
    
    public HierarchyOfReference(string referenceName,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        OrderBy? orderBy, params IHierarchyRequireConstraint?[] requirements) : base(
        new object?[]{referenceName, emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty}, requirements, orderBy)
    {
    }

    public HierarchyOfReference(string[] referenceNames,
        EmptyHierarchicalEntityBehaviour emptyHierarchicalEntityBehaviour,
        OrderBy? orderBy, params IHierarchyRequireConstraint?[] requirements) : base(
        referenceNames.Select(x => x as object).ToArray().Concat(new object?[] {emptyHierarchicalEntityBehaviour})
            .ToArray(), requirements, orderBy)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new HierarchyOfReference(Arguments, children, additionalChildren);
    }
}
