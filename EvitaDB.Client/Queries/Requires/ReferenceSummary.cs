using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Utils;
using FilterBy = EvitaDB.Client.Queries.Filter.FilterBy;
using OrderBy = EvitaDB.Client.Queries.Order.OrderBy;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `referenceSummary` requirement asks the server to compute a summary of every faceted reference of the
/// queried collection - the groups, their options, how many entities carry each option and, at
/// <see cref="FacetStatisticsDepth.Impact"/>, what selecting an option would do to the result.
///
/// It is the 2026 rename of <see cref="FacetSummary"/> and takes the same arguments. The difference is on the
/// wire: the server answers `referenceSummary` with `referenceGroupStatistics`, while `facetSummary` still
/// answers with the deprecated `facetGroupStatistics`. Both are converted into the same
/// <see cref="Models.ExtraResults.FacetSummary"/> extra result, so which constraint a query uses does not
/// change how the response is read.
///
/// Example:
/// <code>
/// referenceSummary(
///     IMPACT,
///     entityFetch(attributeContent("name")),
///     entityGroupFetch(attributeContent("name"))
/// )
/// </code>
/// </summary>
public class ReferenceSummary : AbstractRequireConstraintContainer, IConstraintContainerWithSuffix, IExtraResultRequireConstraint,
    ISeparateEntityContentRequireContainer
{
    private const string SuffixWithHistograms = "withHistograms";

    public FacetStatisticsDepth FacetStatisticsDepth => (FacetStatisticsDepth) Arguments[0]!;

    public EntityFetch? FacetEntityRequirement => Children.OfType<EntityFetch>().FirstOrDefault();

    public EntityGroupFetch? GroupEntityRequirement => Children.OfType<EntityGroupFetch>().FirstOrDefault();

    public FilterBy? FilterBy => AdditionalChildren.OfType<FilterBy>().FirstOrDefault();

    public FilterGroupBy? FilterGroupBy => AdditionalChildren.OfType<FilterGroupBy>().FirstOrDefault();

    public OrderBy? OrderBy => AdditionalChildren.OfType<OrderBy>().FirstOrDefault();

    public OrderGroupBy? OrderGroupBy => AdditionalChildren.OfType<OrderGroupBy>().FirstOrDefault();

    public new bool Applicable => true;

    private ReferenceSummary(object?[] arguments, IRequireConstraint?[] children,
        params IConstraint?[] additionalChildren) : base(arguments, children, additionalChildren)
    {
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

    public ReferenceSummary() : base(new object?[] {Requires.FacetStatisticsDepth.Counts},
        Array.Empty<IEntityContentRequire?>())
    {
    }

    public ReferenceSummary(FacetStatisticsDepth facetStatisticsDepth) : base(new object[] {facetStatisticsDepth})
    {
    }

    public ReferenceSummary(FacetStatisticsDepth facetStatisticsDepth, params IEntityRequire?[] requirements) :
        this(new object[] {facetStatisticsDepth}, requirements)
    {
    }

    public ReferenceSummary(FacetStatisticsDepth facetStatisticsDepth, FilterBy? filterBy,
        FilterGroupBy? filterGroupBy, OrderBy? orderBy, OrderGroupBy? orderGroupBy,
        params IEntityRequire?[] requirements) :
        base(
            new object?[] {facetStatisticsDepth},
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
    /// The `withHistograms` form: entity/group fetches plus one or more <see cref="ReferenceHistogramStatistics"/>.
    /// A distinct signature rather than another `params IEntityRequire[]` overload, because histogram
    /// statistics are a require constraint but not an entity requirement.
    /// </summary>
    public ReferenceSummary(FacetStatisticsDepth facetStatisticsDepth, EntityFetch? entityFetch,
        EntityGroupFetch? entityGroupFetch, params ReferenceHistogramStatistics?[] histogramStatistics) :
        this(
            new object?[] {facetStatisticsDepth},
            new IRequireConstraint?[] {entityFetch, entityGroupFetch}
                .Concat(histogramStatistics.Cast<IRequireConstraint?>())
                .Where(x => x is not null)
                .ToArray())
    {
    }

    /// <summary>
    /// Histogram children switch the rendered name to `referenceSummaryWithHistograms`,
    /// mirroring the Java constraint's SUFFIX_WITH_HISTOGRAMS behaviour.
    /// </summary>
    public string? SuffixIfApplied =>
        Children.OfType<ReferenceHistogramStatistics>().Any() ? SuffixWithHistograms : null;

    public bool ArgumentImplicitForSuffix(object argument) => false;

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new ReferenceSummary(Arguments, children, additionalChildren);
    }
}
