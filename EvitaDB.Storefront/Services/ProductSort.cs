using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Order;
using static EvitaDB.Client.Queries.IQueryConstraints;

namespace EvitaDB.Storefront.Services;

/// <summary>
/// A sort option offered in the listing, mapped to the evitaDB order constraint that implements it.
///
/// Only orderings the demo dataset can actually satisfy are listed: every attribute referenced here is
/// declared sortable on the `Product` schema (`rating`, `ratingVotes`, `orderedQuantity`, `published`,
/// `name`, `code`). <see cref="ProductSort.Available"/> filters the list against the live schema, so an
/// option silently disappears rather than producing a server error if a dataset lacks the attribute.
/// </summary>
/// <param name="Key">stable identifier used in the URL</param>
/// <param name="Label">what the selector shows</param>
/// <param name="RequiredAttribute">sortable attribute this option needs, or null for price ordering</param>
public sealed record ProductSort(string Key, string Label, string? RequiredAttribute)
{
    /// <summary>Builds the order constraint. `name` is localized, so ordering by it is locale-sensitive.</summary>
    public IOrderConstraint? Build() => Key switch
    {
        "price-asc" => PriceNatural(OrderDirection.Asc),
        "price-desc" => PriceNatural(OrderDirection.Desc),
        "name" => AttributeNatural(StorefrontSchema.NameAttribute, OrderDirection.Asc),
        "rating" => AttributeNatural(StorefrontSchema.RatingAttribute, OrderDirection.Desc),
        "bestselling" => AttributeNatural(StorefrontSchema.OrderedQuantityAttribute, OrderDirection.Desc),
        "newest" => AttributeNatural(StorefrontSchema.PublishedAttribute, OrderDirection.Desc),
        // `order` is a Predecessor chain - the merchandiser's hand-picked sequence, which is what a shop
        // shows by default when it has one
        "recommended" => AttributeNatural(StorefrontSchema.OrderAttribute, OrderDirection.Asc),
        _ => PriceNatural(OrderDirection.Asc)
    };

    public static readonly IReadOnlyList<ProductSort> All =
    [
        new("recommended", "recommended", StorefrontSchema.OrderAttribute),
        new("price-asc", "price ↑ low to high", null),
        new("price-desc", "price ↓ high to low", null),
        new("bestselling", "best selling", StorefrontSchema.OrderedQuantityAttribute),
        new("rating", "best rated", StorefrontSchema.RatingAttribute),
        new("newest", "newest", StorefrontSchema.PublishedAttribute),
        new("name", "name A–Z", StorefrontSchema.NameAttribute)
    ];

    /// <summary>Options whose backing attribute the catalog actually declares as sortable.</summary>
    public static IReadOnlyList<ProductSort> Available(IReadOnlyCollection<string> sortableAttributes) =>
        All.Where(x => x.RequiredAttribute is null || sortableAttributes.Contains(x.RequiredAttribute)).ToList();

    public static ProductSort ByKey(string? key, IReadOnlyList<ProductSort> available) =>
        available.FirstOrDefault(x => x.Key == key) ?? available[0];
}
