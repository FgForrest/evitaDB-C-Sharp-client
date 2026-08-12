using Newtonsoft.Json;

namespace EvitaDB.Client.Models.ExtraResults;

public interface IHistogram
{
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public decimal Min { get; }
    public decimal Max { get; }
    public int OverallCount { get; }
    public Bucket[] Buckets { get; }
}

public record Bucket
{
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public decimal Threshold { get; init; }
    public int Occurrences { get; init; }
    public bool Requested { get; init; }

    /// <summary>
    /// Relative frequency of the bucket, for visualization. For a standard histogram it is the percentage of
    /// total occurrences (0-100); for an equalized one it is a normalized density that also accounts for
    /// bucket width, scaled so every bucket sums to 100. Mirrors the fourth component of Java's
    /// `HistogramContract.Bucket`.
    /// </summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public decimal RelativeFrequency { get; init; }

    public Bucket(decimal threshold, int occurrences, bool requested, decimal relativeFrequency = 0m)
    {
        Threshold = threshold;
        Occurrences = occurrences;
        Requested = requested;
        RelativeFrequency = relativeFrequency;
    }
}
