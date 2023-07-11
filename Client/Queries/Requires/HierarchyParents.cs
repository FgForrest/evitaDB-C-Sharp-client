using Client.Utils;

namespace Client.Queries.Requires;

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

    private HierarchyParents(string outputName, IRequireConstraint[] children) : base(ConstraintName,
        new object[] {outputName}, children)
    {
        foreach (IRequireConstraint requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is IHierarchyOutputRequireConstraint or HierarchySiblings or Requires.EntityFetch,
                "Constraint HierarchyParents accepts only HierarchyStopAt, HierarchyStatistics, HierarchySiblings and EntityFetch as inner constraints!");
        }
    }

    public HierarchyParents(string outputName, EntityFetch? entityFetch, params IHierarchyOutputRequireConstraint[] requirements)
        : base(ConstraintName, new object[]{outputName}, new IRequireConstraint[]{entityFetch}.Concat(requirements).ToArray())
    {
    }
    
    public HierarchyParents(string outputName, EntityFetch? entityFetch, HierarchySiblings? hierarchySiblings, params IHierarchyOutputRequireConstraint[] requirements)
        : base(ConstraintName, new object[]{outputName}, new IRequireConstraint[]{entityFetch, hierarchySiblings}.Concat(requirements).ToArray())
    {
    }
    
    public HierarchyParents(string outputName, params IHierarchyOutputRequireConstraint[] requirements)
        : base(ConstraintName, new object[]{outputName}, requirements)
    {
    }
    
    public HierarchyParents(string outputName, HierarchySiblings siblings, params IHierarchyOutputRequireConstraint[] requirements)
        : base(ConstraintName, new object[]{outputName}, new IRequireConstraint[]{siblings}.Concat(requirements).ToArray())
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint[] additionalChildren)
    {
        Assert.IsTrue(additionalChildren.Length == 0,
            "Inner constraints of different type than `require` are not expected.");
        return new HierarchyParents(OutputName, children);
    }
}