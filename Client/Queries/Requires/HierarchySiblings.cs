using Client.Utils;

namespace Client.Queries.Requires;

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
    
    private HierarchySiblings(string? outputName, IRequireConstraint[] children, params IConstraint[] additionalChildren) : base(ConstraintName, new object[] {outputName}, children, additionalChildren)
    {
        foreach (IRequireConstraint requireConstraint in children)
        {
            Assert.IsTrue(requireConstraint is IHierarchyOutputRequireConstraint or Requires.EntityFetch, "Constraint HierarchySiblings accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
        }
        Assert.IsTrue(additionalChildren.Length == 0, "Constraint HierarchySiblings accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
    }
    
    public HierarchySiblings(string? outputName, EntityFetch? entityFetch, params IHierarchyOutputRequireConstraint[] requirements) 
        : base(ConstraintName, outputName is null ? NoArguments :  new object[] {outputName}, new IRequireConstraint[] {entityFetch}.Concat(requirements).ToArray()) 
    {
    }
    
    public HierarchySiblings(string? outputName, params IHierarchyOutputRequireConstraint[] requirements) : base(ConstraintName, outputName is null ? NoArguments :  new object[] {outputName}, requirements)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint[] additionalChildren)
    {
        return new HierarchySiblings(OutputName, children, additionalChildren);
    }

    
}