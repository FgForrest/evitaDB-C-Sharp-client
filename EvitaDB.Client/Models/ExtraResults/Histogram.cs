using System.Globalization;
using System.Text;
using EvitaDB.Client.Utils;
using Newtonsoft.Json;

namespace EvitaDB.Client.Models.ExtraResults;

public class Histogram : IHistogram
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

    /// <summary>
    /// Renders the histogram as the ASCII chart the documentation shows, mirroring Java's
    /// `HistogramContract` string form:
    /// <code>
    /// Histogram[min=0.00, max=5036.00, overall=4063]
    /// 0.00 - 251.80     | 701 ^###### (17.3%)
    /// 251.80 - 503.60   | 467 ^#### (11.5%)
    /// </code>
    /// Ranges are left-padded to a common width and counts right-padded to the widest count, so the bars
    /// line up. The caret marks a bucket the query's `attributeBetween` / `priceBetween` selected.
    /// </summary>
    public override string ToString()
    {
        if (Buckets.Length == 0)
        {
            return "EMPTY HISTOGRAM";
        }

        int maxOccurrences = Buckets.Max(bucket => bucket.Occurrences);
        int countWidth = Math.Max(1, maxOccurrences.ToString(CultureInfo.InvariantCulture).Length);

        string[] ranges = new string[Buckets.Length];
        int rangeWidth = 0;
        for (int i = 0; i < Buckets.Length; i++)
        {
            decimal upperBound = i + 1 < Buckets.Length ? Buckets[i + 1].Threshold : Max;
            ranges[i] = Plain(Buckets[i].Threshold) + " - " + Plain(upperBound);
            rangeWidth = Math.Max(rangeWidth, ranges[i].Length);
        }

        StringBuilder sb = new StringBuilder(256)
            .Append("Histogram[min=").Append(Plain(Buckets[0].Threshold))
            .Append(", max=").Append(Plain(Max))
            .Append(", overall=").Append(OverallCount.ToString(CultureInfo.InvariantCulture))
            .Append(']')
            .Append(Environment.NewLine);

        int overallCount = OverallCount;
        for (int i = 0; i < Buckets.Length; i++)
        {
            Bucket bucket = Buckets[i];

            // one '#' per 2.5% - truncated, but any non-empty bucket still gets a visible mark
            int barSize = (int) (bucket.RelativeFrequency * 0.40m);
            if (bucket.RelativeFrequency > 0m && barSize == 0)
            {
                barSize = 1;
            }

            sb.Append(ranges[i].PadRight(rangeWidth))
                .Append(" | ")
                .Append(bucket.Occurrences.ToString(CultureInfo.InvariantCulture).PadLeft(countWidth))
                .Append(' ');
            if (bucket.Requested)
            {
                sb.Append('^');
            }
            sb.Append('#', barSize);
            if (overallCount > 0)
            {
                // deliberately the plain share of occurrences, not RelativeFrequency - the two differ for
                // equalized histograms, where the frequency also accounts for bucket width
                double percentage = bucket.Occurrences * 100.0d / overallCount;
                sb.Append(" (").Append(percentage.ToString("F1", CultureInfo.InvariantCulture)).Append("%)");
            }
            if (i + 1 < Buckets.Length)
            {
                sb.Append(Environment.NewLine);
            }
        }

        return sb.ToString();
    }

    /// <summary>Plain decimal form - no exponent, no grouping, trailing zeros preserved as received.</summary>
    private static string Plain(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
