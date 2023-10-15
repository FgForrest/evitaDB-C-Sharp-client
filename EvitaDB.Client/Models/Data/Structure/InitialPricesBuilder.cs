using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.Prices;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Builder that is used to create new <see cref="Price"/> instance.
/// Due to performance reasons, there is special implementation for the situation when entity is newly created.
/// In this case we know everything is new and we don't need to closely monitor the changes so this can speed things up.
/// </summary>
public class InitialPricesBuilder : IPricesBuilder
{
    /// <summary>
    /// Entity schema if available
    /// </summary>
    private IEntitySchema EntitySchema { get; }

    private IDictionary<PriceKey, IPrice> Prices { get; } = new Dictionary<PriceKey, IPrice>();
    public PriceInnerRecordHandling? InnerRecordHandling { get; private set; } = PriceInnerRecordHandling.None;
    public int Version => 1;
    public IPrice PriceForSale => throw new ContextMissingException();

    public InitialPricesBuilder(IEntitySchema entitySchema)
    {
        EntitySchema = entitySchema;
    }

    public bool PricesAvailable() => true;
    public IList<IPrice> GetAllPricesForSale(Currency? currency, DateTimeOffset? atTheMoment, params string[] priceListPriority)
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
            .Where(it => !atTheMoment.HasValue || it.Validity == null || it.Validity.ValidFor(atTheMoment.Value))
            .Where(it => !pLists.Any() || pLists.Contains(it.PriceList))
            .ToList();
    }

    public IList<IPrice> GetAllPricesForSale()
    {
        return GetAllPricesForSale(null, null);
    }

    public IPrice? GetPrice(PriceKey priceKey)
    {
        return Prices.TryGetValue(priceKey, out var price) ? price : null;
    }

    public IPrice? GetPrice(int priceId, string priceList, Currency currency)
    {
        return GetPrice(new PriceKey(priceId, priceList, currency));
    }

    public bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode)
    {
        throw new ContextMissingException();
    }

    public IEnumerable<IPrice> GetPrices()
    {
        return Prices.Values;
    }

    public IPricesBuilder SetPrice(int priceId, string priceList, Currency currency, decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax, bool sellable)
    {
        PriceKey priceKey = new PriceKey(priceId, priceList, currency);
        Price thePrice = new Price(priceKey, null, priceWithoutTax, taxRate, priceWithTax, null, sellable);
        AssertPriceNotAmbiguousBeforeAdding(thePrice);
        Prices.Add(priceKey, thePrice);
        return this;
    }

    public IPricesBuilder SetPrice(int priceId, string priceList, Currency currency, int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, bool sellable)
    {
        PriceKey priceKey = new PriceKey(priceId, priceList, currency);
        Price thePrice = new Price(priceKey, innerRecordId, priceWithoutTax, taxRate, priceWithTax, null, sellable);
        AssertPriceNotAmbiguousBeforeAdding(thePrice);
        Prices.Add(priceKey, thePrice);
        return this;
    }

    public IPricesBuilder SetPrice(int priceId, string priceList, Currency currency, decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax, DateTimeRange? validity, bool sellable)
    {
        PriceKey priceKey = new PriceKey(priceId, priceList, currency);
        Price thePrice = new Price(priceKey, null, priceWithoutTax, taxRate, priceWithTax, validity, sellable);
        AssertPriceNotAmbiguousBeforeAdding(thePrice);
        Prices.Add(priceKey, thePrice);
        return this;
    }

    public IPricesBuilder SetPrice(int priceId, string priceList, Currency currency, int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, DateTimeRange? validity, bool sellable)
    {
        PriceKey priceKey = new PriceKey(priceId, priceList, currency);
        Price thePrice = new Price(priceKey, innerRecordId, priceWithoutTax, taxRate, priceWithTax, validity, sellable);
        AssertPriceNotAmbiguousBeforeAdding(thePrice);
        Prices.Add(priceKey, thePrice);
        return this;
    }

    public IPricesBuilder RemovePrice(int priceId, string priceList, Currency currency)
    {
        PriceKey priceKey = new PriceKey(priceId, priceList, currency);
        Prices.Remove(priceKey);
        return this;
    }

    public IPricesBuilder SetPriceInnerRecordHandling(PriceInnerRecordHandling priceInnerRecordHandling)
    {
        InnerRecordHandling = priceInnerRecordHandling;
        return this;
    }

    public IPricesBuilder RemovePriceInnerRecordHandling()
    {
        InnerRecordHandling = PriceInnerRecordHandling.None;
        return this;
    }

    public IPricesBuilder RemoveAllNonTouchedPrices()
    {
        throw new NotSupportedException("This method has no sense when new entity is being created!");
    }

    public IEnumerable<ILocalMutation> BuildChangeSet()
    {
        return InnerRecordHandling is null
            ? ArraySegment<ILocalMutation>.Empty
            : new[] {new SetPriceInnerRecordHandlingMutation(InnerRecordHandling.Value)}
                .Concat<ILocalMutation>(Prices.Select(x =>
                    new UpsertPriceMutation(x.Key, x.Value)));
    }

    public Prices Build()
    {
        return new Prices(EntitySchema, 1, Prices.Values, InnerRecordHandling, Prices.Any());
    }

    /// <summary>
    /// Method throws <see cref="AmbiguousPriceException"/> when there is conflict in prices.
    /// </summary>
    /// <param name="price">price to examine</param>
    /// <exception cref="AmbiguousPriceException">thrown if a conflict between prices was found</exception>
    private void AssertPriceNotAmbiguousBeforeAdding(Price price)
    {
        IPrice? conflictingPrice = GetPrices()
            .Where(it => it.PriceList.Equals(price.PriceList))
            .Where(it => it.Currency.Equals(price.Currency))
            .Where(it => it.PriceId != price.PriceId)
            .Where(it =>
                Equals(it.InnerRecordId, price.InnerRecordId)
            )
            .FirstOrDefault(it => price.Validity != null &&
                (it.Validity is null || price.Validity is null) || it.Validity!.Overlaps(price.Validity!));
        if (conflictingPrice != null)
        {
            throw new AmbiguousPriceException(conflictingPrice, price);
        }
    }
}