using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

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