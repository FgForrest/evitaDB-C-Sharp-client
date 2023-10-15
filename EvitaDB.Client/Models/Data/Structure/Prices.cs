using System.Collections.Immutable;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Queries.Requires;
using Newtonsoft.Json;

namespace EvitaDB.Client.Models.Data.Structure;

public class Prices : IPrices
{
    [JsonIgnore] internal IEntitySchema EntitySchema { get; }
    private bool WithPrice { get; }
    public int Version { get; }
    private ImmutableDictionary<PriceKey, IPrice> PriceIndex { get; }
    public PriceInnerRecordHandling? InnerRecordHandling { get; }
    public IPrice? PriceForSale => throw new ContextMissingException();

    public Prices(IEntitySchema entitySchema, PriceInnerRecordHandling priceInnerRecordHandling)
    {
        EntitySchema = entitySchema;
        WithPrice = entitySchema.WithPrice;
        Version = 1;
        PriceIndex = new Dictionary<PriceKey, IPrice>().ToImmutableDictionary();
        InnerRecordHandling = priceInnerRecordHandling;
    }

    public Prices(IEntitySchema entitySchema, IEnumerable<IPrice> prices,
        PriceInnerRecordHandling priceInnerRecordHandling)
    {
        EntitySchema = entitySchema;
        WithPrice = entitySchema.WithPrice;
        Version = 1;
        PriceIndex = prices.ToDictionary(x => x.Key, x => x).ToImmutableDictionary();
        InnerRecordHandling = priceInnerRecordHandling;
    }

    public Prices(IEntitySchema entitySchema, int version, ICollection<IPrice> prices,
        PriceInnerRecordHandling priceInnerRecordHandling) : this(entitySchema, version, prices,
        priceInnerRecordHandling, entitySchema.WithPrice)
    {
    }

    public Prices(IEntitySchema entitySchema, int version, IEnumerable<IPrice> prices,
        PriceInnerRecordHandling? priceInnerRecordHandling, bool withPrice)
    {
        EntitySchema = entitySchema;
        WithPrice = withPrice;
        Version = version;
        PriceIndex = prices.ToDictionary(x => x.Key, x => x).ToImmutableDictionary();
        InnerRecordHandling = priceInnerRecordHandling;
    }

    public IPrice? GetPrice(PriceKey priceKey) => PriceIndex.TryGetValue(priceKey, out IPrice? price) ? price : null;

    public IPrice? GetPrice(int priceId, string priceList, Currency currency) =>
        PriceIndex.TryGetValue(new PriceKey(priceId, priceList, currency), out IPrice? price) ? price : null;

    internal IPrice? GetPriceWithoutSchemaCheck(PriceKey priceKey)
    {
        return PriceIndex.TryGetValue(priceKey, out IPrice? price) ? price : null;
    }

    public IList<IPrice> GetAllPricesForSale() => GetAllPricesForSale(null, null);
    public bool Empty => PriceIndex.Count == 0;

    public bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode) =>
        throw new ContextMissingException();

    public bool PricesAvailable() => EntitySchema.WithPrice;

    public IList<IPrice> GetAllPricesForSale(Currency? currency, DateTimeOffset? atTheMoment,
        params string[] priceListPriority)
    {
        ISet<string> pLists = new HashSet<string>();
        if (priceListPriority.Length > 0)
        {
            foreach (var priority in priceListPriority)
            {
                pLists.Add(priority);
            }
        }

        return GetPrices()
            .Where(x => x.Sellable)
            .Where(it => currency == null || currency.Equals(it.Currency))
            .Where(it => !atTheMoment.HasValue || (it.Validity == null || it.Validity.ValidFor(atTheMoment.Value)))
            .Where(it => !pLists.Any() || pLists.Contains(it.PriceList))
            .ToList();
    }

    public IEnumerable<IPrice> GetPrices() => PriceIndex.Values;

    public override string ToString()
    {
        if (PricesAvailable())
        {
            List<IPrice> prices = GetPrices().ToList();
            return "selects " + InnerRecordHandling + " from: " +
                   (
                       !prices.Any() ? "no price" : string.Join(", ", prices.Select(x => x.ToString()))
                   );
        }

        return "entity has no prices";
    }
}