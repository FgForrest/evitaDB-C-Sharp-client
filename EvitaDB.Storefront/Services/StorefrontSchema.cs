namespace EvitaDB.Storefront.Services;

/// <summary>
/// Every dataset-specific name the storefront depends on, in one place.
///
/// These were read off the live demo catalog (`demo.evitadb.io`, schema of 2026-08-11). Anything that can be
/// discovered from the schema at runtime is discovered instead - see <see cref="EvitaCatalogContext"/>; what
/// remains here are the names that have to be known before the schema is loaded, or that pick one specific
/// reference/attribute out of many. If the app is pointed at a different dataset, this file is the only edit.
/// </summary>
public static class StorefrontSchema
{
    public const string ProductCollection = "Product";
    public const string CategoryCollection = "Category";
    public const string PriceListCollection = "PriceList";

    /// <summary>Global, non-localized business code carried by nearly every collection.</summary>
    public const string CodeAttribute = "code";

    /// <summary>Localized display name.</summary>
    public const string NameAttribute = "name";

    /// <summary>Localized URL slug - used to address categories and products from the router.</summary>
    public const string UrlAttribute = "url";

    /// <summary>Discriminates BASIC / MASTER / VARIANT products. Filterable in this dataset.</summary>
    public const string ProductTypeAttribute = "productType";

    /// <summary>Sortable attributes backing the sort selector - see <see cref="ProductSort"/>.</summary>
    public const string RatingAttribute = "rating";
    public const string OrderedQuantityAttribute = "orderedQuantity";
    public const string PublishedAttribute = "published";
    public const string OrderAttribute = "order";

    /// <summary>Localized long description. In this dataset it is an attribute, not associated data.</summary>
    public const string DescriptionAttribute = "description";

    /// <summary>Localized teaser shown above the long description and on listing cards.</summary>
    public const string DescriptionShortAttribute = "descriptionShort";

    /// <summary>Global identifiers shown in the detail page's specification block.</summary>
    public const string EanAttribute = "ean";
    public const string CatalogNumberAttribute = "catalogNumber";

    /// <summary>Hierarchical reference from Product to Category; drives `hierarchyWithin` and the nav tree.</summary>
    public const string CategoriesReference = "categories";

    /// <summary>Faceted reference to ParameterValue, grouped by Parameter - the main facet group source.</summary>
    public const string ParameterValuesReference = "parameterValues";

    /// <summary>Faceted reference to Brand (ungrouped).</summary>
    public const string BrandReference = "brand";

    /// <summary>Faceted reference to Tag, grouped by TagCategory.</summary>
    public const string TagsReference = "tags";

    /// <summary>Non-faceted references shown on the product detail page.</summary>
    public const string RelatedProductsReference = "relatedProducts";

    /// <summary>MASTER -> its VARIANT products; rendered on the master's detail page.</summary>
    public const string VariantsReference = "variants";

    /// <summary>
    /// Product types a storefront lists. VARIANT products are deliberately excluded: they are the individual
    /// size/colour combinations, represented in listings by their MASTER (whose price-for-sale evitaDB
    /// computes as the lowest across variants via LOWEST_PRICE inner record handling).
    /// </summary>
    public static readonly string[] ListedProductTypes = ["BASIC", "MASTER"];

    /// <summary>
    /// References rendered in the facet panel, in display order. Intersected with the schema's faceted
    /// references at runtime, so listing one that the dataset does not declare is harmless.
    /// </summary>
    public static readonly string[] FacetPanelReferences =
    [
        ParameterValuesReference,
        BrandReference,
        TagsReference
    ];

    /// <summary>
    /// Attribute histograms to render, in display order. The demo dataset declares 27+ numeric filterable
    /// attributes; showing all of them would be noise, so the panel renders the first few of these that the
    /// schema actually declares.
    /// </summary>
    public static readonly string[] PreferredHistogramAttributes =
    [
        "battery-capacity",
        "display-size",
        "weight",
        "refresh-rate",
        "cpu-frequency"
    ];

    /// <summary>Price list treated as the selling price when a profile does not say otherwise.</summary>
    public const string BasicPriceList = "basic";

    /// <summary>Price list used as the struck-through "recommended" price next to the selling price.</summary>
    public const string ReferencePriceList = "reference";
}
