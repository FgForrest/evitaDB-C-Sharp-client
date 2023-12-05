﻿using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `facetSummary` request triggers the calculation of the FacetSummary containing the facet summary calculation.
/// The calculated facet summary will contain all entity references marked as faceted in the entity schema. The facet
/// summary can be further modified by the facet summary of reference constraint, which allows you to override
/// the general facet summary behavior specified in the generic facet summary require constraint.
/// The faceted property affects the size of the indexes kept in memory and the scale / complexity of the general facet
/// summary (i.e. the summary generated by the facetSummary request). It is recommended to mark only the references used
/// for faceted filtering as faceted to keep the indexes small and the calculation of the facet summary in the user
/// interface fast and simple. The combinatorial complexity of the facet summary is quite high for large datasets, and
/// you may be forced to optimize it by narrowing the summary using the filtering facility or selecting only a few
/// references for the summary.
/// ## Facet calculation rules
/// 1. The facet summary is calculated only for entities that are returned in the current query result.
/// 2. The calculation respects any filter constraints placed outside the 'userFilter' container.
/// 3. The default relation between facets within a group is logical disjunction (logical OR).
/// 4. The default relation between facets in different groups / references is a logical AND.
/// The `facetSummary` requirement triggers the calculation of the <see cref="EvitaDB.Client.Models.ExtraResults.FacetSummary"/> extra result. The facet summary
/// is always computed as a side result of the main entity query and respects any filtering constraints placed on the
/// queried entities.
/// Example:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "e-readers")
///         )
///         entityLocaleEquals("en")
///     ),
///     require(
///         facetSummary(
///             COUNTS,
///             entityFetch(
///                 attributeContent("name")
///             ),
///             entityGroupFetch(
///                 attributeContent("name")
///             )
///         )
///     )
/// )
/// </code>
///
/// <remarks>
/// <para>
/// Filtering facet summary:
/// The facet summary sometimes gets very big, and besides the fact that it is not very useful to show all facet options
/// in the user interface, it also takes a lot of time to calculate it. To limit the facet summary, you can use the
/// <see cref="FilterBy"/> and <see cref="FilterGroupBy"/> (which is the same as filterBy, but it filters the entire facet group
/// instead of individual facets) constraints.
/// If you add the filtering constraints to the facetSummary requirement, you can only refer to filterable properties
/// that are shared by all referenced entities. This may not be feasible in some cases, and you will need to split
/// the generic facetSummary requirement into multiple individual <see cref="FacetSummaryOfReference"/> requirements with
/// specific filters for each reference type.
/// The filter conditions can only target properties on the target entity and cannot target reference attributes in
/// the source entity that are specific to a relationship with the target entity.
/// </para>
/// <para>
/// Ordering facet summary:
/// Typically, the facet summary is ordered in some way to present the most relevant facet options first. The same is
/// true for ordering facet groups. To sort the facet summary items the way you like, you can use the <see cref="OrderBy"/> and
/// <see cref="OrderGroupBy"/> (which is the same as orderBy but it sorts the facet groups instead of the individual facets)
/// constraints.
/// If you add the ordering constraints to the facetSummary requirement, you can only refer to sortable properties that
/// are shared by all referenced entities. This may not be feasible in some cases, and you will need to split the generic
/// facetSummary requirement into multiple individual facetSummaryOfReference requirements with specific ordering
/// constraints for each reference type.
/// The ordering constraints can only target properties on the target entity and cannot target reference attributes in
/// the source entity that are specific to a relationship with the target entity.
/// </para>
/// </remarks>
/// </summary>
public class FacetSummary : AbstractRequireConstraintContainer, IExtraResultRequireConstraint, ISeparateEntityContentRequireContainer
{
    public FacetStatisticsDepth FacetStatisticsDepth => (FacetStatisticsDepth) Arguments[0]!;

    public EntityFetch? FacetEntityRequirement => Children.OfType<EntityFetch>().FirstOrDefault();

    public EntityGroupFetch? GroupEntityRequirement => Children.OfType<EntityGroupFetch>().FirstOrDefault();

    public FilterBy? FilterBy => AdditionalChildren.OfType<FilterBy>().FirstOrDefault();

    public FilterGroupBy? FilterGroupBy => AdditionalChildren.OfType<FilterGroupBy>().FirstOrDefault();

    public OrderBy? OrderBy => AdditionalChildren.OfType<OrderBy>().FirstOrDefault();

    public OrderGroupBy? OrderGroupBy => AdditionalChildren.OfType<OrderGroupBy>().FirstOrDefault();

    public new bool Applicable => true;

    private FacetSummary(object?[] arguments, IRequireConstraint?[] children, params IConstraint?[] additionalChildren) : base(
        arguments, children, additionalChildren)
    {
        Assert.NotNull(FacetStatisticsDepth, "Facet summary requires a facet statistics depth specification.");
        foreach (IRequireConstraint? child in children)
        {
            Assert.IsTrue(child is EntityFetch or EntityGroupFetch,
                "Facet summary accepts only `EntityFetch` and `EntityGroupFetch` constraints.");
        }

        Assert.IsTrue(children.Count(x => x is EntityFetch) <= 1,
            "Facet summary accepts only one `EntityFetch` constraint.");
        Assert.IsTrue(children.Count(x => x is EntityGroupFetch) <= 1,
            "Facet summary accepts only one `EntityGroupFetch` constraint.");
        foreach (IConstraint? child in additionalChildren)
        {
            Assert.IsTrue(child is Filter.FilterBy or Filter.FilterGroupBy or Order.OrderBy or Order.OrderGroupBy,
                "Facet summary accepts only `FilterBy`, `FilterGroupBy`, `OrderBy` and `OrderGroupBy` constraints.");
        }
    }

    public FacetSummary() : base(new object?[] {FacetStatisticsDepth.Counts}, Array.Empty<IEntityContentRequire?>())
    {
    }

    public FacetSummary(FacetStatisticsDepth facetStatisticsDepth) : base(new object[] {facetStatisticsDepth})
    {
    }

    public FacetSummary(FacetStatisticsDepth facetStatisticsDepth, params IEntityRequire?[] requirements) :
        this(new object[] {facetStatisticsDepth}, requirements)
    {
    }

    public FacetSummary(FacetStatisticsDepth facetStatisticsDepth, FilterBy? filterBy, FilterGroupBy? filterGroupBy,
        OrderBy? orderBy, OrderGroupBy? orderGroupBy, params IEntityRequire?[] requirements) :
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

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new FacetSummary(Arguments, children, additionalChildren);
    }
}
