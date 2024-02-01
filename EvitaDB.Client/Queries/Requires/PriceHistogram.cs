using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `priceHistogram` is computed from the price for sale. The interval related constraints - i.e. <see cref="AttributeBetween{T}"/>
/// and <see cref="PriceBetween"/> in the userFilter part are excluded for the sake of histogram calculation. If this weren't
/// the case, the user narrowing the filtered range based on the histogram results would be driven into a narrower and
/// narrower range and eventually into a dead end.
/// The priceType requirement the source price property for the histogram computation. If no requirement, the histogram
/// visualizes the price with tax.
/// Example:
/// <code>
/// priceHistogram(20)
/// </code>
/// </summary>
public class PriceHistogram : AbstractRequireConstraintLeaf, IExtraResultRequireConstraint
{
    public int RequestedBucketCount => (int) Arguments[0]!;
    public HistogramBehavior Behavior => (HistogramBehavior) Arguments[1]!;
    
    private PriceHistogram(params object?[] arguments) : base(arguments)
    {
    }
    
    public PriceHistogram(int requestedBucketCount) : base(requestedBucketCount)
    {
    }
    
    public PriceHistogram(int requestedBucketCount, HistogramBehavior? behavior) 
        : base(requestedBucketCount, behavior ?? HistogramBehavior.Standard)
    {
    }
}
