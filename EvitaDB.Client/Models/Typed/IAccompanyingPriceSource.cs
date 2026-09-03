using EvitaDB.Client.Models.Data;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Exposes the accompanying prices evitaDB computed alongside the price for sale.
/// <remarks>
/// <para>
/// <b>Why this interface exists.</b> The wire already carries them -
/// <c>GrpcEntity.accompanyingPrices</c> is a <c>map&lt;string, GrpcPrice&gt;</c> - but this driver drops
/// them: <c>EntityConverter</c> never reads that field, and <see cref="IPrices"/> has nowhere to put them.
/// Until the driver carries them natively, anything that can supply them implements this interface and the
/// typed layer reads through it.
/// </para>
/// <para>
/// <b>Closing the gap</b> takes two small changes in the driver, after which this interface becomes a thin
/// adapter that keeps working:
/// </para>
/// <list type="number">
/// <item><description>
/// carry the map into the model - give <c>Prices</c> an
/// <c>IReadOnlyDictionary&lt;string, IPrice&gt;</c> and let <c>Entity</c> implement this interface over it;
/// </description></item>
/// <item><description>
/// populate it in <c>EntityConverter.ToEntity</c>, where the <c>new Prices(...)</c> call already converts
/// <c>grpcEntity.Prices</c> and <c>grpcEntity.PriceForSale</c> but ignores
/// <c>grpcEntity.AccompanyingPrices</c>.
/// </description></item>
/// </list>
/// <para>
/// Accompanying prices are a query-time computation, not schema: request them with
/// <c>AccompanyingPriceContent(name, priceLists)</c> (and optionally
/// <c>DefaultAccompanyingPriceLists(...)</c>), both of which this driver already supports.
/// </para>
/// </remarks>
/// </summary>
public interface IAccompanyingPriceSource
{
    /// <summary>
    /// Returns the accompanying price computed under the given name, or <c>null</c> when the query did not
    /// request it.
    /// </summary>
    /// <param name="accompanyingPriceName">
    /// name the query used in <c>AccompanyingPriceContent</c>; <c>"default"</c> when it named none
    /// </param>
    IPrice? GetAccompanyingPrice(string accompanyingPriceName);

    /// <summary>Names of the accompanying prices this entity carries.</summary>
    IReadOnlyCollection<string> GetAccompanyingPriceNames();
}
