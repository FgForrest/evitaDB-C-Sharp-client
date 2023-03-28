using Client.DataTypes;
using Client.Models.Data.Structure;
using Client.Queries.Requires;

namespace Client.Models.Data;

public interface IPrices
{
    Price GetPrice(PriceKey priceKey);
    Price? GetPrice(int priceId, string priceList, Currency currency);
    Price? GetPriceForSale();
    List<Price> GetAllPricesForSale();
    bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode);
    IEnumerable<Price> GetPrices();
}