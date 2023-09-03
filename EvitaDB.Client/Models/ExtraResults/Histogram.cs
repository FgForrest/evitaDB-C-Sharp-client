using System.Text;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.ExtraResults;

public class Histogram : IHistogramContract
{
    public decimal Min => Buckets[0].Threshold;
    public decimal Max { get; }
    public int OverallCount => Buckets.Sum(x => x.Occurrences);
    public Bucket[] Buckets { get; }
    public Histogram(Bucket[] buckets, decimal max)
    {
        Assert.IsTrue(buckets.Length > 0, "Buckets may never be empty!");
        Assert.IsTrue(buckets[^1].Threshold.CompareTo(max) <= 0, "Last bucket must have threshold lower than max!");
        Bucket? lastBucket = null;
        foreach (Bucket bucket in buckets)
        {
            Assert.IsTrue(lastBucket is null || lastBucket.Threshold.CompareTo(bucket.Threshold) < 0,
                "Buckets must have monotonic row of thresholds!");
            lastBucket = bucket;
        }
        Buckets = buckets;
        Max = max;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < Buckets.Length; i++) {
            Bucket bucket = Buckets[i];
            bool hasNext = i + 1 < Buckets.Length;
            sb.Append("[")
                .Append(bucket.Threshold)
                .Append(" - ")
                .Append(hasNext ? Buckets[i + 1].Threshold : Max)
                .Append("]: ")
                .Append(bucket.Occurrences);
            if (hasNext) {
                sb.Append(", ");
            }
        }
        return sb.ToString();
    }
}