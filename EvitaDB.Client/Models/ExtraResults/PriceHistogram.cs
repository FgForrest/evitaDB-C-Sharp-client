namespace EvitaDB.Client.Models.ExtraResults;

public class PriceHistogram : IHistogramContract, IEvitaResponseExtraResult
{
    private readonly IHistogramContract _histogram;
    public decimal Min => _histogram.Min;

    public decimal Max => _histogram.Max;

    public int OverallCount => _histogram.OverallCount;

    public Bucket[] Buckets => _histogram.Buckets;

    public PriceHistogram(IHistogramContract histogram)
    {
        _histogram = histogram;
    }
    
    public override string ToString()
    {
        return _histogram.ToString() ?? string.Empty;
    }
}