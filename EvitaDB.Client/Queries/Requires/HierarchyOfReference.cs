using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

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

    public IHierarchyRequireConstraint?[]? Requirements =>
        Children.Select(x => x as IHierarchyRequireConstraint).ToArray();

    public OrderBy? OrderBy => AdditionalChildren.OfType<OrderBy>().FirstOrDefault();

    public new bool Applicable => Arguments.Length > 0 && GetChildrenCount() > 0;
    
    private HierarchyOfReference(object[] arguments, IRequireConstraint[] children,
        params IConstraint[] additionalChildren)
        : base(arguments, children, additionalChildren)
    {
        foreach (IRequireConstraint child in children)
        {
            Assert.IsTrue(child is IHierarchyRequireConstraint or EntityFetch,
                "Constraint HierarchyOfReference accepts only HierarchyRequireConstraint, EntityFetch or OrderBy as inner constraints!");
        }

        foreach (IConstraint child in additionalChildren)
        {
            Assert.IsTrue(child is OrderBy,
                "Constraint HierarchyOfReference accepts only HierarchyRequireConstraint, EntityFetch or OrderBy as inner constraints!");
        }
    }

    public HierarchyOfReference(string referenceName, EmptyHierarchicalEntityBehaviour emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint[] requirements) : base(
        new object[] {referenceName, emptyHierarchicalEntityBehaviour}, requirements)
    {
    }

    public HierarchyOfReference(string[] referenceNames,
        EmptyHierarchicalEntityBehaviour emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint[] requirements) : base(
        referenceNames.Select(x => x as object).ToArray().Concat(new object[] {emptyHierarchicalEntityBehaviour})
            .ToArray(), requirements)
    {
    }

    public HierarchyOfReference(string referenceName,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint[] requirements) : base(
        new object[] {referenceName, emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty},
        requirements)
    {
    }
    
    public HierarchyOfReference(string referenceName,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        OrderBy? orderBy, params IHierarchyRequireConstraint[] requirements) : base(
        new object[]{referenceName, emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty}, requirements, orderBy)
    {
    }

    public HierarchyOfReference(string[] referenceNames,
        EmptyHierarchicalEntityBehaviour emptyHierarchicalEntityBehaviour,
        OrderBy? orderBy, params IHierarchyRequireConstraint[] requirements) : base(
        referenceNames.Select(x => x as object).ToArray().Concat(new object[] {emptyHierarchicalEntityBehaviour})
            .ToArray(), requirements, orderBy)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint[] additionalChildren)
    {
        return new HierarchyOfReference(Arguments, children, additionalChildren);
    }
}