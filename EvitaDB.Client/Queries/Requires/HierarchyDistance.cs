using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

public class HierarchyDistance : AbstractRequireConstraintLeaf, IHierarchyStopAtRequireConstraint
{
    private const string ConstraintName = "distance";
    
    public int Distance => (int) Arguments[0]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;

    private HierarchyDistance(params object[] arguments) : base(ConstraintName, arguments)
    {
    }
    
    public HierarchyDistance(int distance) : base(ConstraintName, distance)
    {
        Assert.IsTrue(distance > 0, () => new EvitaInvalidUsageException("Distance must be greater than zero."));
    }
}