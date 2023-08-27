using Client.Exceptions;
using Client.Utils;

namespace Client.Queries.Requires;

public class HierarchyLevel : AbstractRequireConstraintLeaf, IHierarchyStopAtRequireConstraint
{
    private const string ConstraintName = "level";
    
    public int Level => (int) Arguments[0]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    
    private HierarchyLevel(params object[] arguments) : base(ConstraintName, arguments)
    {
    }

    public HierarchyLevel(int level) : base(ConstraintName, level)
    {
        Assert.IsTrue(level > 0, () => new EvitaInvalidUsageException("Level must be greater than zero. Level 1 represents root node."));
    }
}