using Client.DataTypes;
using Client.Models.Data.Structure;
using Client.Queries.Requires;

namespace Client.Models.Data;

public interface IPrices : IVersioned
{
    PriceInnerRecordHandling InnerRecordHandling { get; }
    bool PricesAvailable { get; }
    IPrice GetPrice(PriceKey priceKey);
    IPrice? GetPrice(int priceId, string priceList, Currency currency);
    IPrice? GetPriceForSale();
    List<IPrice> GetAllPricesForSale();
    bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode);
    IEnumerable<IPrice> GetPrices();
}