using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

public class HierarchyStopAt : AbstractRequireConstraintContainer, IHierarchyOutputRequireConstraint
{
    private const string ConstraintName = "stopAt";
    
    public IHierarchyStopAtRequireConstraint StopAtDefinition => (IHierarchyStopAtRequireConstraint) Children[0];
    public HierarchyLevel? Level => Children.FirstOrDefault(x => x is HierarchyLevel) as HierarchyLevel;
    public HierarchyDistance? Distance => Children.FirstOrDefault(x => x is HierarchyDistance) as HierarchyDistance;
    public HierarchyNode? Node => Children.FirstOrDefault(x => x is HierarchyNode) as HierarchyNode;
    public new bool Applicable => GetChildrenCount() >= 1;

    private HierarchyStopAt(IRequireConstraint[] children) : base(ConstraintName, children)
    {
    }

    public HierarchyStopAt(IHierarchyStopAtRequireConstraint stopAtDefinition) : base(ConstraintName, stopAtDefinition)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint[] additionalChildren)
    {
        foreach (IRequireConstraint? requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is IHierarchyStopAtRequireConstraint or EntityFetch,
                "Constraint HierarchyChildren accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!"
            );
        }
        return new HierarchyStopAt(children);
    }
}