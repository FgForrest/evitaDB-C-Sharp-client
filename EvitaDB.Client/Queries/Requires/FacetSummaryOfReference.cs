using Client.Queries.Filter;
using Client.Queries.Order;
using Client.Utils;

namespace Client.Queries.Requires;

public class FacetSummaryOfReference : AbstractRequireConstraintContainer, ISeparateEntityContentRequireContainer,
    IExtraResultRequireConstraint
{
    public string ReferenceName => (string) Arguments[0]!;
    public FacetStatisticsDepth FacetStatisticsDepth => (FacetStatisticsDepth) Arguments[1]!;

    public EntityFetch? FacetEntityRequirement => Children.OfType<EntityFetch>().FirstOrDefault();

    public EntityGroupFetch? GroupEntityRequirement => Children.OfType<EntityGroupFetch>().FirstOrDefault();

    public FilterBy? FilterBy => AdditionalChildren.OfType<FilterBy>().FirstOrDefault();

    public FilterGroupBy? FilterGroupBy => AdditionalChildren.OfType<FilterGroupBy>().FirstOrDefault();

    public OrderBy? OrderBy => AdditionalChildren.OfType<OrderBy>().FirstOrDefault();

    public OrderGroupBy? OrderGroupBy => AdditionalChildren.OfType<OrderGroupBy>().FirstOrDefault();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length >= 1;

    private FacetSummaryOfReference(object[] arguments, IRequireConstraint[] children,
        params IConstraint[] additionalChildren) : base(
        arguments, children, additionalChildren)
    {
        Assert.NotNull(ReferenceName, "Facet summary requires reference name.");
        Assert.NotNull(FacetStatisticsDepth, "Facet summary requires a facet statistics depth specification.");
        foreach (IRequireConstraint child in children)
        {
            Assert.IsTrue(child is EntityFetch or EntityGroupFetch,
                "Facet summary accepts only `EntityFetch` and `EntityGroupFetch` constraints.");
        }

        Assert.IsTrue(children.Count(x => x is EntityFetch) <= 1,
            "Facet summary accepts only one `EntityFetch` constraint.");
        Assert.IsTrue(children.Count(x => x is EntityGroupFetch) <= 1,
            "Facet summary accepts only one `EntityGroupFetch` constraint.");
        foreach (IConstraint child in additionalChildren)
        {
            Assert.IsTrue(child is Filter.FilterBy or Filter.FilterGroupBy or Order.OrderBy or Order.OrderGroupBy,
                "Facet summary accepts only `FilterBy`, `FilterGroupBy`, `OrderBy` and `OrderGroupBy` constraints.");
        }
    }

    public FacetSummaryOfReference(string referenceName) : base(new object[]
        {referenceName, FacetStatisticsDepth.Counts})
    {
    }

    public FacetSummaryOfReference(string referenceName, FacetStatisticsDepth facetStatisticsDepth,
        params IEntityRequire[] requirements) :
        this(new object[] {referenceName, facetStatisticsDepth}, requirements)
    {
    }

    public FacetSummaryOfReference(string referenceName, FacetStatisticsDepth facetStatisticsDepth, FilterBy filterBy,
        FilterGroupBy filterGroupBy, OrderBy orderBy, OrderGroupBy orderGroupBy, params IEntityRequire[] requirements) :
        base(
            new object[] {referenceName, facetStatisticsDepth},
            requirements, filterBy, filterGroupBy, orderBy, orderGroupBy)
    {
        Assert.IsTrue(requirements.Length <= 2,
            $"Expected maximum number of 2 entity requirements. Found {requirements.Length}.");
        if (requirements.Length == 2)
        {
            Assert.IsTrue(requirements[0].GetType() != requirements[1].GetType(),
                "Cannot have two same entity requirements.");
        }
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint[] additionalChildren)
    {
        return new FacetSummaryOfReference(Arguments, children, additionalChildren);
    }
}