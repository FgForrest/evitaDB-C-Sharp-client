using Client.Exceptions;
using Client.Queries.Filter;
using Client.Utils;

namespace Client.Queries.Requires;

public class HierarchyNode : AbstractRequireConstraintContainer, IHierarchyStopAtRequireConstraint
{
    private const string ConstraintName = "node";
    
    public FilterBy FilterBy => GetAdditionalChild(typeof(FilterBy)) as FilterBy ?? throw new InvalidOperationException("Hierarchy node expects FilterBy as its single inner constraint!");
    public new bool Applicable => AdditionalChildren.Length == 1;
    
    public HierarchyNode(FilterBy filterBy) : base(ConstraintName, Array.Empty<object>(), Array.Empty<IRequireConstraint>(), filterBy)
    {
    }
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint[] additionalChildren)
    {
        Assert.IsTrue(children.Length == 0, "Inner constraints of different type than FilterBy are not expected.");
        Assert.IsTrue(additionalChildren.Length == 1, "HierarchyNode expect FilterBy inner constraint!");
        foreach (IConstraint constraint in additionalChildren)
        {
            Assert.IsTrue(constraint is FilterBy, "Constraint HierarchyNode accepts only FilterBy as inner constraint!");
        }
        return new HierarchyNode((FilterBy) additionalChildren[0]);
    }
}