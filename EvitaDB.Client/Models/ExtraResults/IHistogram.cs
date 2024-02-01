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
    
    public Bucket(decimal threshold, int occurrences, bool requested)
    {
        Threshold = threshold;
        Occurrences = occurrences;
        Requested = requested;
    }
}
