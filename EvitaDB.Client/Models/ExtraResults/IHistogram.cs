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
    public int Index { get; init; }
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public decimal Threshold { get; init; }
    public int Occurrences { get; init; }
    public bool Requested { get; init; }
    
    public Bucket(int index, decimal threshold, int occurrences, bool requested)
    {
        Index = index;
        Threshold = threshold;
        Occurrences = occurrences;
        Requested = requested;
    }
}
