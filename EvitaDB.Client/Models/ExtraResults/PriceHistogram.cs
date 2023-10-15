namespace EvitaDB.Client.Models.ExtraResults;

public class PriceHistogram : IHistogram, IEvitaResponseExtraResult
{
    private readonly IHistogram _histogram;
    public decimal Min => _histogram.Min;

    public decimal Max => _histogram.Max;

    public int OverallCount => _histogram.OverallCount;

    public Bucket[] Buckets => _histogram.Buckets;

    public PriceHistogram(IHistogram histogram)
    {
        _histogram = histogram;
    }
    
    public override string ToString()
    {
        return _histogram.ToString() ?? string.Empty;
    }
}