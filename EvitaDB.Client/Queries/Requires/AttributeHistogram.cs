using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `attributeHistogram` can be computed from any filterable attribute whose type is numeric. The histogram is
/// computed only from the attributes of elements that match the current mandatory part of the filter. The interval
/// related constraints - i.e. <see cref="AttributeBetween{T}"/> and <see cref="PriceBetween"/> in the userFilter part are excluded for
/// the sake of histogram calculation. If this weren't the case, the user narrowing the filtered range based on
/// the histogram results would be driven into a narrower and narrower range and eventually into a dead end.
/// Example:
/// <code>
/// attributeHistogram(5, "width", "height")
/// </code>
/// </summary>
public class AttributeHistogram : AbstractRequireConstraintLeaf, IExtraResultRequireConstraint
{
    private AttributeHistogram(params object[] arguments) : base(arguments)
    {
    }
    
    public AttributeHistogram(int requestedBucketCount, HistogramBehavior? behavior, params string[] attributeNames) 
        : base(new object[]{requestedBucketCount,behavior ?? HistogramBehavior.Standard}.Concat(attributeNames).ToArray())
    {
    }
    
    public AttributeHistogram(int requestedBucketCount, params string[] attributeNames) 
        : base(new object[]{requestedBucketCount, HistogramBehavior.Standard}.Concat(attributeNames).ToArray())
    {
    }
    
    public int RequestedBucketCount => (int) Arguments[0]!;
    public HistogramBehavior Behavior => (HistogramBehavior) Arguments[1]!;
    public string[] AttributeNames => Arguments.Skip(2).Select(obj => (string) obj!).ToArray();
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 2;
}
