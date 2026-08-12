using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `histogramStatistics` requirement asks a <see cref="ReferenceSummary"/> to compute histograms for the
/// named histogram indexes declared on the reference schema, in addition to the group and option statistics.
///
/// It is nested inside `referenceSummary` / `referenceSummaryOfReference`, which then render with the
/// `withHistograms` suffix:
/// <code>
/// referenceSummaryOfReferenceWithHistograms(
///     "parameterValues", NONE,
///     entityFetch(attributeContent("code")),
///     entityGroupFetch(attributeContent("code")),
///     histogramStatistics(20, "intervalParameterValues")
/// )
/// </code>
///
/// Arguments are the requested bucket count, the <see cref="HistogramBehavior"/> and then the index names;
/// an optional <see cref="EntityFetch"/> child controls the richness of the entities the histogram refers to.
/// </summary>
public class ReferenceHistogramStatistics : AbstractRequireConstraintContainer
{
    private const string ConstraintName = "histogramStatistics";

    private ReferenceHistogramStatistics(object?[] arguments, IRequireConstraint?[] children,
        params IConstraint?[] additionalChildren) : base(ConstraintName, arguments, children, additionalChildren)
    {
        foreach (IRequireConstraint? child in children)
        {
            Assert.IsTrue(child is EntityFetch,
                "Histogram statistics accepts only `EntityFetch` constraint.");
        }
        Assert.IsTrue(children.Count(x => x is EntityFetch) <= 1,
            "Histogram statistics accepts only one `EntityFetch` constraint.");
    }

    public ReferenceHistogramStatistics(int requestedBucketCount, params string[] indexNames)
        : this(requestedBucketCount, HistogramBehavior.Standard, null, indexNames)
    {
    }

    public ReferenceHistogramStatistics(int requestedBucketCount, HistogramBehavior? behavior,
        params string[] indexNames)
        : this(requestedBucketCount, behavior, null, indexNames)
    {
    }

    public ReferenceHistogramStatistics(int requestedBucketCount, EntityFetch? entityFetch,
        params string[] indexNames)
        : this(requestedBucketCount, HistogramBehavior.Standard, entityFetch, indexNames)
    {
    }

    public ReferenceHistogramStatistics(int requestedBucketCount, HistogramBehavior? behavior,
        EntityFetch? entityFetch, params string[] indexNames)
        : base(
            ConstraintName,
            // bucket count and behaviour first, then the index names - mirrors Java's argument merge
            new object?[] {requestedBucketCount, behavior ?? HistogramBehavior.Standard}
                .Concat(indexNames.Cast<object?>()).ToArray(),
            entityFetch is null ? [] : [entityFetch]
        )
    {
    }

    public int RequestedBucketCount => (int) Arguments[0]!;

    public HistogramBehavior Behavior => (HistogramBehavior) Arguments[1]!;

    public string[] IndexNames => Arguments.Skip(2).OfType<string>().ToArray();

    public EntityFetch? EntityFetch => Children.OfType<EntityFetch>().FirstOrDefault();

    public new bool Applicable => Arguments.Length >= 2;

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new ReferenceHistogramStatistics(Arguments, children, additionalChildren);
    }
}
