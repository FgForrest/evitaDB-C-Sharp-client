using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `referenceSummaryOfReference` requirement computes the reference summary for one specific reference,
/// overriding, for that reference only, the constraints given by a generic <see cref="ReferenceSummary"/>.
/// Combine the two to state common requirements once and redefine them where they are insufficient.
///
/// It is the 2026 rename of <see cref="FacetSummaryOfReference"/> and takes the same arguments; the server
/// answers it with `referenceGroupStatistics` rather than the deprecated `facetGroupStatistics`, and both are
/// converted into the same <see cref="Models.ExtraResults.FacetSummary"/> extra result.
///
/// Calculation rules (identical to the generic requirement):
/// 1. The summary is calculated only for entities returned by the current query.
/// 2. The calculation respects every filter constraint placed outside the `userFilter` container.
/// 3. The default relation between options within a group is logical disjunction (OR).
/// 4. The default relation between options in different groups / references is logical conjunction (AND).
///
/// Example:
/// <code>
/// referenceSummaryOfReference(
///     "parameterValues",
///     IMPACT,
///     entityFetch(attributeContent("name")),
///     entityGroupFetch(attributeContent("name"))
/// )
/// </code>
/// </summary>
public class ReferenceSummaryOfReference : AbstractRequireConstraintContainer, IConstraintContainerWithSuffix, ISeparateEntityContentRequireContainer,
    IExtraResultRequireConstraint
{
    private const string SuffixWithHistograms = "withHistograms";

    public string ReferenceName => (string) Arguments[0]!;
    public FacetStatisticsDepth FacetStatisticsDepth => (FacetStatisticsDepth) Arguments[1]!;

    public EntityFetch? FacetEntityRequirement => Children.OfType<EntityFetch>().FirstOrDefault();

    public EntityGroupFetch? GroupEntityRequirement => Children.OfType<EntityGroupFetch>().FirstOrDefault();

    public FilterBy? FilterBy => AdditionalChildren.OfType<FilterBy>().FirstOrDefault();

    public FilterGroupBy? FilterGroupBy => AdditionalChildren.OfType<FilterGroupBy>().FirstOrDefault();

    public OrderBy? OrderBy => AdditionalChildren.OfType<OrderBy>().FirstOrDefault();

    public OrderGroupBy? OrderGroupBy => AdditionalChildren.OfType<OrderGroupBy>().FirstOrDefault();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length >= 1;

    private ReferenceSummaryOfReference(object?[] arguments, IRequireConstraint?[] children,
        params IConstraint?[] additionalChildren) : base(
        arguments, children, additionalChildren)
    {
        Assert.NotNull(ReferenceName, "Reference summary requires reference name.");
        Assert.NotNull(FacetStatisticsDepth, "Reference summary requires a statistics depth specification.");
        foreach (IRequireConstraint? child in children)
        {
            Assert.IsTrue(child is EntityFetch or EntityGroupFetch or ReferenceHistogramStatistics,
                "Reference summary accepts only `EntityFetch`, `EntityGroupFetch` and `HistogramStatistics` constraints.");
        }

        Assert.IsTrue(children.Count(x => x is EntityFetch) <= 1,
            "Reference summary accepts only one `EntityFetch` constraint.");
        Assert.IsTrue(children.Count(x => x is EntityGroupFetch) <= 1,
            "Reference summary accepts only one `EntityGroupFetch` constraint.");
        foreach (IConstraint? child in additionalChildren)
        {
            Assert.IsTrue(child is Filter.FilterBy or Filter.FilterGroupBy or Order.OrderBy or Order.OrderGroupBy,
                "Reference summary accepts only `FilterBy`, `FilterGroupBy`, `OrderBy` and `OrderGroupBy` constraints.");
        }
    }

    public ReferenceSummaryOfReference(string referenceName) : base(new object[]
        {referenceName, FacetStatisticsDepth.Counts})
    {
    }

    public ReferenceSummaryOfReference(string referenceName, FacetStatisticsDepth facetStatisticsDepth,
        params IEntityRequire[] requirements) :
        this(new object[] {referenceName, facetStatisticsDepth}, requirements)
    {
    }

    public ReferenceSummaryOfReference(string referenceName, FacetStatisticsDepth facetStatisticsDepth, FilterBy? filterBy,
        FilterGroupBy? filterGroupBy, OrderBy? orderBy, OrderGroupBy? orderGroupBy, params IEntityRequire?[] requirements) :
        base(
            new object?[] {referenceName, facetStatisticsDepth},
            requirements, filterBy, filterGroupBy, orderBy, orderGroupBy)
    {
        Assert.IsTrue(requirements.Length <= 2,
            $"Expected maximum number of 2 entity requirements. Found {requirements.Length}.");
        if (requirements.Length == 2)
        {
            Assert.IsTrue(requirements[0]!.GetType() != requirements[1]!.GetType(),
                "Cannot have two same entity requirements.");
        }
    }

    /// <summary>
    /// The `withHistograms` form - see the matching constructor on <see cref="ReferenceSummary"/>.
    /// </summary>
    public ReferenceSummaryOfReference(string referenceName, FacetStatisticsDepth facetStatisticsDepth,
        EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch,
        params ReferenceHistogramStatistics?[] histogramStatistics) :
        this(
            new object?[] {referenceName, facetStatisticsDepth},
            new IRequireConstraint?[] {entityFetch, entityGroupFetch}
                .Concat(histogramStatistics.Cast<IRequireConstraint?>())
                .Where(x => x is not null)
                .ToArray())
    {
    }

    /// <summary>
    /// Histogram children switch the rendered name to `referenceSummaryOfReferenceWithHistograms`,
    /// mirroring the Java constraint's SUFFIX_WITH_HISTOGRAMS behaviour.
    /// </summary>
    public string? SuffixIfApplied =>
        Children.OfType<ReferenceHistogramStatistics>().Any() ? SuffixWithHistograms : null;

    public bool ArgumentImplicitForSuffix(object argument) => false;

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new ReferenceSummaryOfReference(Arguments, children, additionalChildren);
    }
}
