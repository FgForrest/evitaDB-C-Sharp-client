namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// This enumeration describes the behaviour of <see cref="AttributeHistogram"/> and <see cref="PriceHistogram"/> calculation.
/// </summary>
public enum HistogramBehavior
{
    /// <summary>
    /// Histogram always contains the number of buckets you asked for. This is the default behaviour.
    /// </summary>
    Standard,
    /// <summary>
    /// Histogram will never contain more buckets than you asked for, but may contain less when the data is scarce and
    /// there would be big gaps (empty buckets) between buckets. This leads to more compact histograms, which provide
    /// better user experience.
    /// </summary>
    Optimized
}
