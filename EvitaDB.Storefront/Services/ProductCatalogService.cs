using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.ExtraResults;
using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
// only the container type is needed by name from the filter namespace - aliased rather than imported
// wholesale, to keep the constraint factories unambiguous
using UserFilter = EvitaDB.Client.Queries.Filter.UserFilter;
using static EvitaDB.Client.Queries.IQueryConstraints;
// `FacetSummary`, `PriceHistogram` and `AttributeHistogram` name BOTH a require constraint and an extra result.
// Both namespaces are needed here, so the extra-result types are aliased - the bare names keep meaning the
// constraints, which is what the query builder reads most naturally.
using FacetSummaryResult = EvitaDB.Client.Models.ExtraResults.FacetSummary;
using PriceHistogramResult = EvitaDB.Client.Models.ExtraResults.PriceHistogram;
using AttributeHistogramResult = EvitaDB.Client.Models.ExtraResults.AttributeHistogram;

namespace EvitaDB.Storefront.Services;

/// <summary>Result of a listing query - the page plus everything the facet panel needs.</summary>
/// <param name="Products">entities on the current page</param>
/// <param name="TotalRecordCount">total matching products</param>
/// <param name="PageNumber">1-based page number</param>
/// <param name="PageSize">page size</param>
/// <param name="FacetSummary">facet groups and their statistics; null when the server returned none</param>
/// <param name="PriceHistogram">price distribution; null when no prices matched</param>
/// <param name="AttributeHistograms">numeric attribute distributions, keyed by attribute name</param>
public sealed record ProductListing(
    IReadOnlyList<ISealedEntity> Products,
    int TotalRecordCount,
    int PageNumber,
    int PageSize,
    FacetSummaryResult? FacetSummary,
    PriceHistogramResult? PriceHistogram,
    AttributeHistogramResult? AttributeHistograms
)
{
    public int PageCount => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);
}

/// <summary>
/// Every storefront query lives here, so the query shapes can be reviewed in one place.
/// All methods are async: the sync facades on the driver block, which deadlocks on WebAssembly.
/// </summary>
public sealed class ProductCatalogService
{
    private readonly EvitaCatalogContext _catalog;
    private readonly StorefrontState _state;

    public ProductCatalogService(EvitaCatalogContext catalog, StorefrontState state)
    {
        _catalog = catalog;
        _state = state;
    }

    /// <summary>
    /// Category tree for the navigation, with the number of matching products per node.
    ///
    /// Asked on the Product collection (not on Category) via `hierarchyOfReference`, so `queriedEntityCount`
    /// counts products rather than categories.
    /// </summary>
    public async Task<IReadOnlyList<LevelInfo>> GetCategoryTreeAsync(int depth = 2,
        CancellationToken cancellationToken = default)
    {
        EvitaEntityReferenceResponse response = await _catalog.ExecuteAsync(session =>
            session.QueryAsync<EvitaEntityReferenceResponse, EntityReference>(
                Query(
                    Collection(StorefrontSchema.ProductCollection),
                    FilterBy(
                        And(
                            EntityLocaleEquals(_state.Locale),
                            PriceInCurrency(_state.Currency),
                            PriceInPriceLists(_state.SelectedPriceLists.ToArray()),
                            PriceValidInNow()
                        )
                    ),
                    Require(
                        // no product bodies needed - only the hierarchy extra result
                        Page(1, 1),
                        HierarchyOfReference(
                            StorefrontSchema.CategoriesReference,
                            FromRoot(
                                "menu",
                                EntityFetch(AttributeContent(
                                    StorefrontSchema.CodeAttribute,
                                    StorefrontSchema.NameAttribute,
                                    StorefrontSchema.UrlAttribute)),
                                StopAt(Level(depth)),
                                Statistics(StatisticsType.ChildrenCount, StatisticsType.QueriedEntityCount)
                            )
                        ),
                        DataInLocales(_state.Locale)
                    )
                ),
                cancellationToken
            ), cancellationToken).ConfigureAwait(false);

        // NOTE: the two-argument GetReferenceHierarchy indexes its dictionaries directly and throws
        // KeyNotFoundException when the reference or output name is absent - go through the nullable
        // single-argument overload instead, so an empty result renders as an empty menu.
        Hierarchy? hierarchy = response.GetExtraResult<Hierarchy>();
        IDictionary<string, List<LevelInfo>>? byOutputName =
            hierarchy?.GetReferenceHierarchy(StorefrontSchema.CategoriesReference);
        return byOutputName is not null && byOutputName.TryGetValue("menu", out List<LevelInfo>? levels)
            ? levels
            : [];
    }

    /// <summary>
    /// The core listing query: products in a category, with facet summary, price histogram and attribute
    /// histograms computed for the facet panel.
    /// </summary>
    /// <param name="categoryCode">category to browse; null lists the whole catalog</param>
    public async Task<ProductListing> GetListingAsync(
        string? categoryCode,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        string[] histogramAttributes = ResolveHistogramAttributes();

        EvitaEntityResponse response = await _catalog.ExecuteAsync(session =>
            session.QueryAsync<EvitaEntityResponse, ISealedEntity>(
                Query(
                    Collection(StorefrontSchema.ProductCollection),
                    FilterBy(
                        And(
                            EntityLocaleEquals(_state.Locale),
                            categoryCode is null
                                ? null
                                : HierarchyWithin(
                                    StorefrontSchema.CategoriesReference,
                                    AttributeEquals(StorefrontSchema.CodeAttribute, categoryCode)),
                            // VARIANT products are represented in listings by their MASTER
                            AttributeInSet(StorefrontSchema.ProductTypeAttribute,
                                StorefrontSchema.ListedProductTypes),
                            PriceInCurrency(_state.Currency),
                            PriceInPriceLists(_state.SelectedPriceLists.ToArray()),
                            PriceValidInNow(),
                            BuildUserFilter()
                        )
                    ),
                    OrderBy(_state.Sort.Build()),
                    Require(
                        Page(pageNumber, pageSize),
                        EntityFetch(
                            AttributeContent(
                                StorefrontSchema.CodeAttribute,
                                StorefrontSchema.NameAttribute,
                                StorefrontSchema.UrlAttribute,
                                StorefrontSchema.ProductTypeAttribute,
                                StorefrontSchema.RatingAttribute,
                                StorefrontSchema.OrderedQuantityAttribute,
                                // teaser rendered on the card
                                StorefrontSchema.DescriptionShortAttribute),
                            PriceContentRespectingFilter(),
                            // brand name for the card - group fetch is not needed, brand is ungrouped
                            ReferenceContentWithAttributes(
                                StorefrontSchema.BrandReference,
                                EntityFetch(AttributeContent(
                                    StorefrontSchema.CodeAttribute, StorefrontSchema.NameAttribute)))
                        ),
                        // one facet summary per faceted reference we render
                        FacetSummaryOfReference(
                            StorefrontSchema.ParameterValuesReference, FacetStatisticsDepth.Impact,
                            EntityFetch(AttributeContentAll()), EntityGroupFetch(AttributeContentAll())),
                        FacetSummaryOfReference(
                            StorefrontSchema.BrandReference, FacetStatisticsDepth.Impact,
                            EntityFetch(AttributeContentAll())),
                        FacetSummaryOfReference(
                            StorefrontSchema.TagsReference, FacetStatisticsDepth.Impact,
                            EntityFetch(AttributeContentAll()), EntityGroupFetch(AttributeContentAll())),
                        PriceHistogram(20),
                        histogramAttributes.Length == 0 ? null : AttributeHistogram(20, histogramAttributes),
                        DataInLocales(_state.Locale),
                        PriceType(_state.PriceMode)
                    )
                ),
                cancellationToken
            ), cancellationToken).ConfigureAwait(false);

        return new ProductListing(
            response.RecordData.ToList(),
            response.RecordPage.TotalRecordCount,
            pageNumber,
            pageSize,
            response.GetExtraResult<FacetSummaryResult>(),
            response.GetExtraResult<PriceHistogramResult>(),
            response.GetExtraResult<AttributeHistogramResult>()
        );
    }

    /// <summary>
    /// Single product with everything the detail page renders: all attributes and associated data, all prices,
    /// parameters (with their groups), tags, brand, related products and the category breadcrumb.
    /// </summary>
    public async Task<ISealedEntity?> GetProductAsync(string productCode,
        CancellationToken cancellationToken = default)
    {
        EvitaEntityResponse response = await _catalog.ExecuteAsync(session =>
            session.QueryAsync<EvitaEntityResponse, ISealedEntity>(
                Query(
                    Collection(StorefrontSchema.ProductCollection),
                    FilterBy(
                        And(
                            EntityLocaleEquals(_state.Locale),
                            AttributeEquals(StorefrontSchema.CodeAttribute, productCode),
                            PriceInCurrency(_state.Currency),
                            PriceInPriceLists(_state.SelectedPriceLists.ToArray()),
                            PriceValidInNow()
                        )
                    ),
                    Require(
                        Page(1, 1),
                        EntityFetch(
                            AttributeContentAll(),
                            AssociatedDataContentAll(),
                            PriceContentAll(),
                            ReferenceContentWithAttributes(
                                StorefrontSchema.ParameterValuesReference,
                                EntityFetch(AttributeContentAll()),
                                EntityGroupFetch(AttributeContentAll())),
                            ReferenceContentWithAttributes(
                                StorefrontSchema.TagsReference,
                                EntityFetch(AttributeContentAll())),
                            ReferenceContentWithAttributes(
                                StorefrontSchema.BrandReference,
                                EntityFetch(AttributeContentAll())),
                            ReferenceContentWithAttributes(
                                StorefrontSchema.RelatedProductsReference,
                                EntityFetch(AttributeContentAll())),
                            // A MASTER's individual size/colour combinations. Variants usually share the
                            // master's name, so their own parameterValues are fetched too - the detail page
                            // shows the parameters that actually differ between them as the label.
                            ReferenceContentWithAttributes(
                                StorefrontSchema.VariantsReference,
                                EntityFetch(
                                    AttributeContentAll(),
                                    PriceContentRespectingFilter(),
                                    ReferenceContentWithAttributes(
                                        StorefrontSchema.ParameterValuesReference,
                                        EntityFetch(AttributeContent(
                                            StorefrontSchema.CodeAttribute, StorefrontSchema.NameAttribute)),
                                        EntityGroupFetch(AttributeContent(
                                            StorefrontSchema.CodeAttribute, StorefrontSchema.NameAttribute))))),
                            // breadcrumb source. NOTE: no `hierarchyContent` here - Product is not a
                            // hierarchical collection in this dataset (Category is), so requesting it would
                            // fail; the ancestors are read from the referenced Category entities instead.
                            ReferenceContentWithAttributes(
                                StorefrontSchema.CategoriesReference,
                                EntityFetch(AttributeContent(
                                    StorefrontSchema.CodeAttribute,
                                    StorefrontSchema.NameAttribute,
                                    StorefrontSchema.UrlAttribute)))
                        ),
                        DataInLocales(_state.Locale),
                        PriceType(_state.PriceMode)
                    )
                ),
                cancellationToken
            ), cancellationToken).ConfigureAwait(false);

        return response.RecordData.FirstOrDefault();
    }

    /// <summary>
    /// Builds the `userFilter` container holding the shopper's own choices.
    ///
    /// Two rules make this correct, and both are easy to get wrong:
    ///
    /// 1. Facet selections and the price range MUST sit inside `userFilter`. The facet summary and the price
    ///    histogram are computed while ignoring this container, so moving them out makes every impact figure
    ///    self-referential.
    /// 2. Exactly ONE `facetHaving` per reference, carrying every selected id for that reference. evitaDB then
    ///    applies OR within a facet group and AND between groups - which is the behaviour a shop wants, and is
    ///    the engine default (no facetGroupsConjunction/Disjunction needed). Emitting one `facetHaving` per
    ///    ticked checkbox instead ANDs everything together: measured on the demo dataset, two options of one
    ///    group yield 1291 products in a single constraint but only 1093 when split across two.
    /// </summary>
    private UserFilter? BuildUserFilter()
    {
        List<IFilterConstraint?> constraints = [];

        foreach ((string referenceName, HashSet<int> selected) in _state.SelectedFacets)
        {
            if (selected.Count == 0)
            {
                continue;
            }
            constraints.Add(
                FacetHaving(referenceName, EntityPrimaryKeyInSet(selected.OrderBy(x => x).ToArray()))
            );
        }

        if (_state.PriceFrom is not null || _state.PriceTo is not null)
        {
            constraints.Add(PriceBetween(_state.PriceFrom, _state.PriceTo));
        }

        // attribute histogram ranges behave exactly like the price range - inside userFilter, so the
        // histograms themselves stay stable while the user drags them
        foreach ((string attributeName, (decimal? from, decimal? to)) in _state.AttributeRanges)
        {
            // `attributeBetween` takes non-nullable bounds (its `T?` resolves to plain `decimal` under the
            // `where T : IComparable` constraint), so an open-ended range degrades to a one-sided comparison
            constraints.Add((from, to) switch
            {
                (not null, not null) => AttributeBetween<decimal>(attributeName, from.Value, to.Value),
                (not null, null) => AttributeGreaterThanEquals<decimal>(attributeName, from.Value),
                (null, not null) => AttributeLessThanEquals<decimal>(attributeName, to.Value),
                _ => null
            });
        }

        return constraints.Count == 0 ? null : UserFilter(constraints.ToArray());
    }

    /// <summary>
    /// Preferred histogram attributes, intersected with what the schema actually declares as numeric and
    /// filterable, capped so the sidebar stays readable.
    /// </summary>
    private string[] ResolveHistogramAttributes()
    {
        HashSet<string> available = new(_catalog.HistogramAttributes, StringComparer.Ordinal);
        string[] preferred = StorefrontSchema.PreferredHistogramAttributes
            .Where(available.Contains)
            .Take(3)
            .ToArray();
        return preferred.Length > 0
            ? preferred
            : _catalog.HistogramAttributes.Take(3).ToArray();
    }
}
