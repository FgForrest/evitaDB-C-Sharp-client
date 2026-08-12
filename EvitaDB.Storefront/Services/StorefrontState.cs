using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Storefront.Services;

/// <summary>
/// Everything a production storefront would infer from the domain, the request locale or the customer profile,
/// made explicit and switchable. Changing any of it re-runs the current query.
///
/// The whole selection round-trips through the URL query string (<see cref="ToQueryString"/> /
/// <see cref="ApplyQueryString"/>) so a filtered listing can be shared as a link.
/// </summary>
public sealed class StorefrontState
{
    private readonly EvitaCatalogContext _catalog;
    private bool _suspendNotifications;

    public StorefrontState(EvitaCatalogContext catalog)
    {
        _catalog = catalog;
        Locale = Pick(catalog.Locales, x => x.TwoLetterISOLanguageName == "en") ?? catalog.Locales.FirstOrDefault()
            ?? CultureInfo.GetCultureInfo("en");
        Currency = Pick(catalog.Currencies, x => x.CurrencyCode == "EUR") ?? catalog.Currencies.FirstOrDefault()
            ?? new Currency("EUR");
        AvailableSorts = ProductSort.Available(catalog.SortableAttributes);
        Sort = AvailableSorts[0];
        SelectedPriceLists = DefaultPriceLists(catalog.PriceLists);
    }

    /// <summary>Raised whenever a selection changes; pages re-query on it.</summary>
    public event Action? OnChanged;

    public CultureInfo Locale { get; private set; }
    public Currency Currency { get; private set; }

    /// <summary>
    /// Price lists in priority order, fed straight to `priceInPriceLists`. Order matters: the first list that
    /// has a price for an entity wins, so a customer can hold several at once (e.g. a VIP tier plus `basic`
    /// as the fallback, with `reference` last to provide the struck-through comparison price).
    /// </summary>
    public List<string> SelectedPriceLists { get; private set; }

    public IReadOnlyList<ProductSort> AvailableSorts { get; }

    public ProductSort Sort { get; private set; }

    /// <summary>Page sizes offered in the listing.</summary>
    public static readonly int[] PageSizes = [12, 24, 48, 100];

    /// <summary>Products per page. Part of the shared URL so a link reproduces the same view.</summary>
    public int PageSize { get; private set; } = 24;

    public void SetPageSize(int pageSize)
    {
        if (PageSize == pageSize || !PageSizes.Contains(pageSize)) return;
        PageSize = pageSize;
        Notify();
    }

    /// <summary>Whether displayed and filtered prices include tax.</summary>
    public QueryPriceMode PriceMode { get; private set; } = QueryPriceMode.WithTax;

    /// <summary>Selected facet primary keys, per reference name.</summary>
    public Dictionary<string, HashSet<int>> SelectedFacets { get; } = new(StringComparer.Ordinal);

    /// <summary>Price range from the histogram slider, in the current currency; null when untouched.</summary>
    public decimal? PriceFrom { get; private set; }
    public decimal? PriceTo { get; private set; }

    /// <summary>
    /// Ranges picked on the attribute histograms, keyed by attribute name. Applied as `attributeBetween`
    /// inside the same `userFilter` container as the facets and the price range.
    /// </summary>
    public Dictionary<string, (decimal? From, decimal? To)> AttributeRanges { get; } = new(StringComparer.Ordinal);

    public (decimal? From, decimal? To) GetAttributeRange(string attributeName) =>
        AttributeRanges.TryGetValue(attributeName, out (decimal? From, decimal? To) range) ? range : (null, null);

    public void SetAttributeRange(string attributeName, decimal? from, decimal? to)
    {
        if (from is null && to is null)
        {
            AttributeRanges.Remove(attributeName);
        }
        else
        {
            AttributeRanges[attributeName] = (from, to);
        }
        Notify();
    }

    public void SetLocale(CultureInfo locale)
    {
        if (Equals(Locale, locale)) return;
        Locale = locale;
        Notify();
    }

    public void SetCurrency(Currency currency)
    {
        if (Currency.CurrencyCode == currency.CurrencyCode) return;
        Currency = currency;
        // thresholds from the previous currency's histogram are meaningless now
        ClearPriceRange();
        Notify();
    }

    public void SetSort(ProductSort sort)
    {
        if (Sort.Key == sort.Key) return;
        Sort = sort;
        Notify();
    }

    public bool IsPriceListSelected(string code) => SelectedPriceLists.Contains(code);

    /// <summary>Adds or removes a price list, preserving the catalog's own ordering as the priority order.</summary>
    public void TogglePriceList(string code)
    {
        if (!SelectedPriceLists.Remove(code))
        {
            SelectedPriceLists.Add(code);
            SelectedPriceLists = _catalog.PriceLists.Where(SelectedPriceLists.Contains).ToList();
        }
        if (SelectedPriceLists.Count == 0)
        {
            // a query without a price list cannot compute a price for sale at all
            SelectedPriceLists = DefaultPriceLists(_catalog.PriceLists);
        }
        ClearPriceRange();
        Notify();
    }

    public void SetPriceMode(QueryPriceMode priceMode)
    {
        if (PriceMode == priceMode) return;
        PriceMode = priceMode;
        ClearPriceRange();
        Notify();
    }

    public bool IsFacetSelected(string referenceName, int primaryKey) =>
        SelectedFacets.TryGetValue(referenceName, out HashSet<int>? selected) && selected.Contains(primaryKey);

    public void ToggleFacet(string referenceName, int primaryKey)
    {
        HashSet<int> selected = SelectedFacets.TryGetValue(referenceName, out HashSet<int>? existing)
            ? existing
            : SelectedFacets[referenceName] = [];
        if (!selected.Add(primaryKey))
        {
            selected.Remove(primaryKey);
        }
        if (selected.Count == 0)
        {
            SelectedFacets.Remove(referenceName);
        }
        Notify();
    }

    public void SetPriceRange(decimal? from, decimal? to)
    {
        PriceFrom = from;
        PriceTo = to;
        Notify();
    }

    public bool HasActiveFilters =>
        SelectedFacets.Count > 0 || PriceFrom is not null || PriceTo is not null || AttributeRanges.Count > 0;

    public void ClearFilters()
    {
        SelectedFacets.Clear();
        AttributeRanges.Clear();
        PriceFrom = null;
        PriceTo = null;
        Notify();
    }

    private void ClearPriceRange()
    {
        PriceFrom = null;
        PriceTo = null;
        // thresholds are currency/price-list/tax dependent; attribute ranges are not, so they survive
    }

    private void Notify()
    {
        if (!_suspendNotifications)
        {
            OnChanged?.Invoke();
        }
    }

    // ---------------------------------------------------------------- URL round-trip

    private const string FacetPrefix = "f.";
    private const string RangePrefix = "r.";

    /// <summary>
    /// Serializes the whole selection into a query string, so the current listing can be shared as a link.
    /// Only non-default values are emitted, keeping ordinary URLs short.
    /// </summary>
    public string ToQueryString()
    {
        List<string> parts = [];

        void Add(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
        }

        if (!Equals(Locale, _catalog.Locales.FirstOrDefault())) Add("locale", Locale.Name);
        Add("currency", Currency.CurrencyCode);
        if (!SelectedPriceLists.SequenceEqual(DefaultPriceLists(_catalog.PriceLists)))
        {
            Add("priceLists", string.Join(",", SelectedPriceLists));
        }
        if (Sort.Key != AvailableSorts[0].Key) Add("sort", Sort.Key);
        if (PageSize != 24) Add("size", PageSize.ToString(CultureInfo.InvariantCulture));
        if (PriceMode != QueryPriceMode.WithTax) Add("tax", "excluded");
        if (PriceFrom is not null || PriceTo is not null)
        {
            Add("price", $"{Fmt(PriceFrom)}-{Fmt(PriceTo)}");
        }
        foreach ((string reference, HashSet<int> ids) in SelectedFacets.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            Add(FacetPrefix + reference, string.Join(",", ids.OrderBy(x => x)));
        }
        foreach ((string attribute, (decimal? from, decimal? to)) in
                 AttributeRanges.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            Add(RangePrefix + attribute, $"{Fmt(from)}-{Fmt(to)}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    /// <summary>
    /// Restores a selection produced by <see cref="ToQueryString"/>. Unknown or malformed values are ignored
    /// rather than throwing - a shared link must never be able to break the page.
    /// </summary>
    public void ApplyQueryString(string? queryString)
    {
        Dictionary<string, string> values = ParseQuery(queryString);

        _suspendNotifications = true;
        try
        {
            SelectedFacets.Clear();
            AttributeRanges.Clear();
            PriceFrom = null;
            PriceTo = null;

            if (values.TryGetValue("locale", out string? locale))
            {
                CultureInfo? match = _catalog.Locales.FirstOrDefault(x => x.Name == locale);
                if (match is not null) Locale = match;
            }
            if (values.TryGetValue("currency", out string? currency))
            {
                Currency? match = _catalog.Currencies.FirstOrDefault(x => x.CurrencyCode == currency);
                if (match is not null) Currency = match;
            }
            if (values.TryGetValue("priceLists", out string? priceLists))
            {
                List<string> requested = priceLists.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Where(_catalog.PriceLists.Contains).ToList();
                if (requested.Count > 0) SelectedPriceLists = requested;
            }
            if (values.TryGetValue("sort", out string? sort))
            {
                Sort = ProductSort.ByKey(sort, AvailableSorts);
            }
            PageSize = values.TryGetValue("size", out string? size)
                       && int.TryParse(size, out int parsedSize) && PageSizes.Contains(parsedSize)
                ? parsedSize
                : 24;
            PriceMode = values.TryGetValue("tax", out string? tax) && tax == "excluded"
                ? QueryPriceMode.WithoutTax
                : QueryPriceMode.WithTax;
            if (values.TryGetValue("price", out string? price))
            {
                (PriceFrom, PriceTo) = ParseRange(price);
            }

            foreach ((string key, string value) in values)
            {
                if (key.StartsWith(FacetPrefix, StringComparison.Ordinal))
                {
                    HashSet<int> ids = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => int.TryParse(x, out int id) ? id : (int?)null)
                        .Where(x => x is not null).Select(x => x!.Value).ToHashSet();
                    if (ids.Count > 0) SelectedFacets[key[FacetPrefix.Length..]] = ids;
                }
                else if (key.StartsWith(RangePrefix, StringComparison.Ordinal))
                {
                    (decimal? from, decimal? to) = ParseRange(value);
                    if (from is not null || to is not null)
                    {
                        AttributeRanges[key[RangePrefix.Length..]] = (from, to);
                    }
                }
            }
        }
        finally
        {
            _suspendNotifications = false;
        }
    }

    private static Dictionary<string, string> ParseQuery(string? queryString)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(queryString))
        {
            return values;
        }
        foreach (string pair in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = pair.IndexOf('=');
            if (separator <= 0) continue;
            values[Uri.UnescapeDataString(pair[..separator])] = Uri.UnescapeDataString(pair[(separator + 1)..]);
        }
        return values;
    }

    private static (decimal? From, decimal? To) ParseRange(string value)
    {
        string[] bounds = value.Split('-', 2);
        return bounds.Length != 2 ? (null, null) : (ParseDecimal(bounds[0]), ParseDecimal(bounds[1]));
    }

    private static decimal? ParseDecimal(string? text) =>
        decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : null;

    private static string Fmt(decimal? value) =>
        value?.ToString("0.####", CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Default basket of price lists: the selling list plus, when present, the `reference` list that supplies
    /// the struck-through comparison price.
    /// </summary>
    private static List<string> DefaultPriceLists(IReadOnlyList<string> available)
    {
        List<string> defaults = [];
        if (available.Contains(StorefrontSchema.BasicPriceList)) defaults.Add(StorefrontSchema.BasicPriceList);
        if (available.Contains(StorefrontSchema.ReferencePriceList)) defaults.Add(StorefrontSchema.ReferencePriceList);
        if (defaults.Count == 0 && available.Count > 0) defaults.Add(available[0]);
        return defaults;
    }

    private static T? Pick<T>(IReadOnlyList<T> source, Func<T, bool> predicate) where T : class =>
        source.FirstOrDefault(predicate);
}
