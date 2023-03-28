using System.Collections.Immutable;
using Client.DataTypes;
using Client.Exceptions;
using Client.Queries.Requires;

namespace Client.Models.Data.Structure;

public class Prices : IPrices
{
    public int Version { get; }
    private IDictionary<PriceKey, Price> PriceIndex { get; }
    public PriceInnerRecordHandling InnerRecordHandling { get; init; }
    
    public Prices(PriceInnerRecordHandling priceInnerRecordHandling) {
        Version = 1;
        PriceIndex = new Dictionary<PriceKey, Price>().ToImmutableDictionary();
        InnerRecordHandling = priceInnerRecordHandling;
    }

    public Prices(ICollection<Price> prices, PriceInnerRecordHandling priceInnerRecordHandling) {
        Version = 1;
        PriceIndex = prices.ToDictionary(x=>x.Key, x=>x).ToImmutableDictionary();
        InnerRecordHandling = priceInnerRecordHandling;
    }

    public Prices(int version, ICollection<Price> prices, PriceInnerRecordHandling priceInnerRecordHandling) {
        Version = version;
        PriceIndex = prices.ToDictionary(x=>x.Key, x=>x).ToImmutableDictionary();
        InnerRecordHandling = priceInnerRecordHandling;
    }
    
    public Price GetPrice(PriceKey priceKey) => PriceIndex[priceKey];
    public Price? GetPrice(int priceId, string priceList, Currency currency) => PriceIndex[new PriceKey(priceId, priceList, currency)];
    public Price? GetPriceForSale() => throw new ContextMissingException();
    public Price? GetPriceForSaleIfAvailable(string priceList) => null;
    public List<Price> GetAllPricesForSale() => new();
    public bool Empty => PriceIndex.Count == 0;
    public bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode) => throw new ContextMissingException();
    public List<Price> GetAllPricesForSale(Currency? currency, DateTimeOffset? atTheMoment, params string[] priceListPriority) {
        ISet<string> pLists = new HashSet<string>();
        if (priceListPriority.Length > 0) {
            foreach (var priority in priceListPriority)
            {
                pLists.Add(priority);
            }
        }
        return GetPrices()
            .Where(x=>x.Sellable)
            .Where(it => currency == null || currency.Equals(it.Currency))
            .Where(it => !atTheMoment.HasValue || (it.Validity == null || it.Validity.ValidFor(atTheMoment.Value)))
            .Where(it => !pLists.Any() || pLists.Contains(it.PriceList))
            .ToList();
    }
    
    public IEnumerable<Price> GetPrices() => PriceIndex.Values;
}