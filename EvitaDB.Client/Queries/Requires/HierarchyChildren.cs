using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

public class HierarchyChildren : AbstractRequireConstraintContainer, IHierarchyRequireConstraint
{
    private const string ConstraintName = "children";

    public string OutputName => (string) Arguments[0]!;
    public HierarchyStopAt? StopAt => Children.FirstOrDefault(x => x is HierarchyStopAt) as HierarchyStopAt;
    public EntityFetch? EntityFetch => Children.FirstOrDefault(x => x is EntityFetch) as EntityFetch;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    
    private HierarchyChildren(string outputName, IRequireConstraint[] children, params IConstraint[] additionalChildren)
        : base(ConstraintName, new object[] {outputName}, children, additionalChildren)
    {
        foreach (IRequireConstraint requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is IHierarchyOutputRequireConstraint or Requires.EntityFetch,
                "Constraint HierarchyChildren accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!"
            );
        }
        Assert.IsTrue(AdditionalChildren.Length == 0, "Constraint HierarchyChildren accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
    }

    public HierarchyChildren(string outputName, EntityFetch entityFetch, params IHierarchyOutputRequireConstraint[] requirements) : base(ConstraintName, new object[]{outputName}, new IRequireConstraint[]{entityFetch}.Concat(requirements).ToArray())
    {
    }
    
    public HierarchyChildren(string outputName, params IHierarchyOutputRequireConstraint[] requirements) : base(ConstraintName, new object[]{outputName}, requirements)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint[] additionalChildren)
    {
        return new HierarchyChildren(OutputName, children, additionalChildren);
    }
}