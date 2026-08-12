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
    Optimized,
    /// <summary>
    /// Histogram buckets are computed so that they contain roughly the same number of records.
    /// </summary>
    Equalized,
    /// <summary>
    /// Combination of <see cref="Equalized"/> and <see cref="Optimized"/> - equalized buckets that may be fewer
    /// than requested when the data doesn't fill them.
    /// </summary>
    EqualizedOptimized
}
