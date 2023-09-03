using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

public class HierarchyFromRoot : AbstractRequireConstraintContainer, IHierarchyRequireConstraint
{
    private const string ConstraintName = "fromRoot";

    public string OutputName => (string) Arguments[0]!;
    public HierarchyStopAt? StopAt => (HierarchyStopAt?) Children.FirstOrDefault(x => x is HierarchyStopAt);
    public EntityFetch? EntityFetch => (EntityFetch?) Children.FirstOrDefault(x => x is EntityFetch);
    public HierarchyStatistics? Statistics => (HierarchyStatistics?) Children.FirstOrDefault(x => x is HierarchyStatistics);
    public IHierarchyOutputRequireConstraint[] OutputRequirements => Children.Where(x => x.GetType().IsAssignableFrom(typeof(IHierarchyOutputRequireConstraint))).Cast<IHierarchyOutputRequireConstraint>().ToArray();
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    
    public HierarchyFromRoot(string outputName, IRequireConstraint[] children, params IConstraint[] additionalChildren) : base(ConstraintName, new object[] {outputName}, children, additionalChildren)
    {
        foreach (IRequireConstraint requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is IHierarchyOutputRequireConstraint or Requires.EntityFetch,
                "Constraint HierarchyFromRoot accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!"
            );
        }
        Assert.IsTrue(AdditionalChildren.Length == 0, "Constraint HierarchyFromRoot accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
    }
    
    public HierarchyFromRoot(string outputName, EntityFetch? entityFetch, params IHierarchyOutputRequireConstraint[] requirements) : base(ConstraintName, new object[] {outputName}, new IRequireConstraint[] {entityFetch}.Concat(requirements).ToArray()) 
    {
    }

    public HierarchyFromRoot(string outputName, params IHierarchyOutputRequireConstraint[] requirements) : base(ConstraintName, new object[] {outputName}, requirements)
    {
    }
    
    public HierarchyFromRoot(string outputName) : base(ConstraintName, new object[] {outputName})
    {
    }
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint[] additionalChildren)
    {
        return new HierarchyFromRoot(OutputName, children, additionalChildren);
    }
}