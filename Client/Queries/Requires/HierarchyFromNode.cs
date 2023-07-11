using Client.Utils;

namespace Client.Queries.Requires;

public class HierarchyFromNode : AbstractRequireConstraintContainer, IHierarchyRequireConstraint
{
    private const string ConstraintName = "fromNode";
    public string? OutputName => (string?) Arguments[0];

    public HierarchyNode FromNode => (HierarchyNode) (Children.FirstOrDefault(x => x is HierarchyNode) ??
                                                      throw new InvalidOperationException(
                                                          "The HierarchyNode inner constraint unexpectedly not found!"));

    public HierarchyStopAt? StopAt => (HierarchyStopAt?) Children.FirstOrDefault(x => x is HierarchyStopAt);
    public EntityFetch? EntityFetch => (EntityFetch?) Children.FirstOrDefault(x => x is EntityFetch);

    public HierarchyStatistics? Statistics =>
        (HierarchyStatistics?) Children.FirstOrDefault(x => x is HierarchyStatistics);

    public IHierarchyOutputRequireConstraint[] OutputRequirements => Children
        .Where(x => x.GetType().IsAssignableFrom(typeof(IHierarchyOutputRequireConstraint)))
        .Cast<IHierarchyOutputRequireConstraint>().ToArray();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1 && Children.Length >= 1;

    private HierarchyFromNode(string outputName, IRequireConstraint[] children, params IConstraint[] additionalChildren)
        : base(ConstraintName, new object[] {outputName}, children, additionalChildren)
    {
        foreach (IRequireConstraint requireConstraint in children)
        {
            Assert.IsTrue(
                requireConstraint is HierarchyNode or IHierarchyOutputRequireConstraint or Requires.EntityFetch,
                "Constraint HierarchyFromNode accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!");
        }

        Assert.IsTrue(
            additionalChildren.Length == 0,
            "Constraint HierarchyFromNode accepts only HierarchyStopAt, HierarchyStatistics and EntityFetch as inner constraints!"
        );
    }

    public HierarchyFromNode(string outputName, HierarchyNode node, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint[]? requirements)
        : base(ConstraintName, new object[] {outputName},
            new IRequireConstraint[] {node, entityFetch}.Concat(requirements).ToArray())
    {
    }

    public HierarchyFromNode(string outputName, HierarchyNode fromNode) : base(ConstraintName,
        new object[] {outputName}, fromNode)
    {
    }

    public HierarchyFromNode(string outputName, HierarchyNode fromNode,
        params IHierarchyOutputRequireConstraint[] requirements) : base(ConstraintName, new object[] {outputName},
        new IRequireConstraint[] {fromNode}.Concat(requirements).ToArray())
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint[] additionalChildren)
    {
        return new HierarchyFromNode(OutputName, children, additionalChildren);
    }
}