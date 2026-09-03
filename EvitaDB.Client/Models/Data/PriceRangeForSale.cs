namespace EvitaDB.Client.Models.Data;

/// <summary>
/// The full range of prices for which an entity could be sold: the cheapest, the most expensive, and the selling
/// price itself.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart of <c>io.evitadb.api.requestResponse.data.PriceRangeForSale</c>, carried on the wire as
/// <c>priceForSaleMin</c> / <c>priceForSaleMax</c> beside <c>priceForSale</c>. All three bounds are computed by
/// the server with identical currency, valid-in and price-list filters, so they are directly comparable.
/// </para>
/// <para>
/// What the bounds mean depends on the entity's <see cref="PriceInnerRecordHandling"/>:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="PriceInnerRecordHandling.None"/> — all three are the same price. An ordinary product has no range.
/// </description></item>
/// <item><description>
/// <see cref="PriceInnerRecordHandling.LowestPrice"/> — a product with variants.
/// <see cref="LowestPrice"/> equals <see cref="PriceForSale"/> (the cheapest per-variant selling price) and
/// <see cref="HighestPrice"/> is the most expensive one. This is the "from €5 to €6.50" a listing shows.
/// </description></item>
/// <item><description>
/// <see cref="PriceInnerRecordHandling.Sum"/> — a product set. The bounds are the cheapest and most expensive
/// <em>component</em> price, while <see cref="PriceForSale"/> is their sum. So for a set the bounds do not
/// bracket the selling price, and presenting them as a range would be wrong.
/// </description></item>
/// </list>
/// <para>
/// <b>Why this is read from the server rather than computed here.</b> Deriving it client-side means selecting a
/// selling price per inner record — which is a ranking by price-list priority, not a filter. This driver's
/// <see cref="IPrices.GetAllPricesForSale()"/> deliberately does neither: it returns matching indexed prices,
/// unranked and ungrouped. Reimplementing the ranking would duplicate the algorithm documented in evitaDB's
/// price-for-sale deep dive, including the cases where a lower-priority price list is skipped entirely, and it
/// would drift from the server the first time that algorithm changed.
/// </para>
/// </remarks>
/// <param name="LowestPrice">Cheapest per-inner-record price satisfying the query's price filter.</param>
/// <param name="HighestPrice">Most expensive per-inner-record price satisfying the same filter.</param>
/// <param name="PriceForSale">The selling price — identical to <see cref="Structure.Entity.PriceForSale"/>.</param>
public sealed record PriceRangeForSale(IPrice LowestPrice, IPrice HighestPrice, IPrice PriceForSale)
{
    /// <summary>
    /// True when the bounds differ, i.e. there is a range worth showing.
    /// </summary>
    /// <remarks>
    /// A product with variants that are all priced the same has a range whose ends are equal, and a storefront
    /// should print one price rather than "from €26 to €26". evitaDB's own documentation uses exactly that case
    /// (`Jumper X-Mas Deer`) as the example.
    /// </remarks>
    public bool Spans => LowestPrice.PriceWithTax != HighestPrice.PriceWithTax;

    /// <summary>Builds the range when the server sent all three prices; null otherwise.</summary>
    /// <remarks>
    /// Null rather than an exception, mirroring Java's <c>getPriceRangeForSaleIfAvailable()</c>: a query without
    /// price constraints legitimately produces no range, and that is not a fault in the caller.
    /// </remarks>
    internal static PriceRangeForSale? Of(IPrice? priceForSale, IPrice? lowest, IPrice? highest) =>
        priceForSale is null || lowest is null || highest is null
            ? null
            : new PriceRangeForSale(lowest, highest, priceForSale);
}
