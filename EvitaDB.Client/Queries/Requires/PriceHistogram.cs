namespace EvitaDB.Client.Queries.Requires;

public class PriceHistogram : AbstractRequireConstraintLeaf, IExtraResultRequireConstraint
{
    public int RequestedBucketCount => (int) Arguments[0]!;
    
    private PriceHistogram(params object[] arguments) : base(arguments)
    {
    }
    
    public PriceHistogram(int requestedBucketCount) : base(requestedBucketCount)
    {
    }
}