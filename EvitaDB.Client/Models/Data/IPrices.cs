using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Models.Data;

public interface IPrices : IVersioned
{
    PriceInnerRecordHandling InnerRecordHandling { get; }
    public IPrice? PriceForSale { get; }
    bool PricesAvailable { get; }
    IPrice GetPrice(PriceKey priceKey);
    IPrice? GetPrice(int priceId, string priceList, Currency currency);
    bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode);
    IEnumerable<IPrice> GetPrices();
    
    static bool AnyPriceDifferBetween(IPrices first, IPrices second) {
        IEnumerable<IPrice> thisValues = first.PricesAvailable ? first.GetPrices() : new List<IPrice>();
        IEnumerable<IPrice> otherValues = second.PricesAvailable ? second.GetPrices() : new List<IPrice>();

        var enumerable = thisValues.ToList();
        if (enumerable.Count() != otherValues.Count()) {
            return true;
        }

        return enumerable
            .Any(it => it.DiffersFrom(second.GetPrice(it.PriceId, it.PriceList, it.Currency)));
    }
}