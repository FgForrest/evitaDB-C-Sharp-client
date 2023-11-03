namespace EvitaDB.Client.Models.ExtraResults;

public interface IHistogram
{
    public decimal Min { get; }
    public decimal Max { get; }
    public int OverallCount { get; }
    public Bucket[] Buckets { get; }
}

public record Bucket(int Index, decimal Threshold, int Occurrences, bool Requested);