using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.Prices;
using EvitaDB.Client.Models.Data.Structure.Predicates;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Builder that is used to alter existing <see cref="Prices"/>. Prices is immutable object so there is need for another object
/// that would simplify the process of updating its contents. This is why the builder class exists.
/// </summary>
public class ExistingPricesBuilder : IPricesBuilder
{
    private IDictionary<PriceKey, PriceMutation> PriceMutations { get; }
    private IEntitySchema EntitySchema { get; }
    private Prices BasePrices { get; }
    private SetPriceInnerRecordHandlingMutation? PriceInnerRecordHandlingEntityMutation { get; set; }
    private bool RemoveAllNonModifiedPrices { get; set; }
    public int Version => BasePrices.Version;
    public PricePredicate PricePredicate { get; }

    public PriceInnerRecordHandling? InnerRecordHandling => PriceInnerRecordHandlingEntityMutation is not null
        ? PriceInnerRecordHandlingEntityMutation.MutateLocal(EntitySchema, BasePrices).InnerRecordHandling
        : BasePrices.InnerRecordHandling;

    public IPrice? PriceForSale
    {
        //TODO TPO: HOW TO DO THIS WITHOUT PREDICATES?
        get
        {
            if (BasePrices.PricesAvailable())
            {
                return BasePrices.PriceForSale;
            }

            return null;
        }
    }

    public ExistingPricesBuilder(
        IEntitySchema entitySchema,
        Prices prices,
        PricePredicate pricePredicate
    )
    {
        BasePrices = prices;
        EntitySchema = entitySchema;
        PriceMutations = new Dictionary<PriceKey, PriceMutation>();
        PricePredicate = pricePredicate;
    }

    /// <summary>
    /// Method allows adding specific mutation on the fly.
    /// </summary>
    /// <param name="setPriceInnerRecordHandlingMutation">price inner record handling mutation to add</param>
    public void AddMutation(SetPriceInnerRecordHandlingMutation setPriceInnerRecordHandlingMutation)
    {
        PriceInnerRecordHandlingEntityMutation = setPriceInnerRecordHandlingMutation;
    }

    /// <summary>
    /// Method allows adding specific mutation on the fly.
    /// </summary>
    /// <param name="localMutation">mutation to add</param>
    /// <exception cref="EvitaInternalError">thrown when unknown price mutation has been passed</exception>
    public void AddMutation(PriceMutation localMutation)
    {
        if (localMutation is UpsertPriceMutation upsertPriceMutation)
        {
            PriceKey priceKey = upsertPriceMutation.PriceKey;
            AssertPriceNotAmbiguousBeforeAdding(
                new Price(
                    priceKey,
                    upsertPriceMutation.InnerRecordId,
                    upsertPriceMutation.PriceWithoutTax,
                    upsertPriceMutation.TaxRate,
                    upsertPriceMutation.PriceWithTax,
                    upsertPriceMutation.Validity,
                    upsertPriceMutation.Sellable
                )
            );
            PriceMutations.Add(priceKey, upsertPriceMutation);
        }
        else if (localMutation is RemovePriceMutation removePriceMutation)
        {
            PriceKey priceKey = removePriceMutation.PriceKey;
            Assert.NotNull(BasePrices.GetPriceWithoutSchemaCheck(priceKey), "Price " + priceKey + " doesn't exist!");
            RemovePriceMutation mutation = new RemovePriceMutation(priceKey);
            PriceMutations.Add(priceKey, mutation);
        }
        else
        {
            throw new EvitaInternalError("Unknown Evita price mutation: `" + localMutation.GetType() + "`!");
        }
    }

    /// <summary>
    /// Method throws an exception when there is conflict in prices.
    /// </summary>
    /// <param name="price">price to check</param>
    /// <exception cref="AmbiguousPriceException">thrown when there is conflict in prices</exception>
    private void AssertPriceNotAmbiguousBeforeAdding(IPrice price)
    {
        // check whether new price doesn't conflict with original prices
        IPrice? conflictingPrice = BasePrices.GetPrices()
            .Where(x => !x.Dropped)
            .Where(it => it.PriceList.Equals(price.PriceList))
            .Where(it => it.Currency.Equals(price.Currency))
            .Where(it => it.PriceId != price.PriceId)
            .Where(it => Equals(it.InnerRecordId, price.InnerRecordId))
            .Where(it => it.Validity == null || it.Validity is not null && price.Validity is not null &&
                it.Validity.Overlaps(price.Validity)
            )
            // the conflicting prices don't play role if they're going to be removed in the same update
            .FirstOrDefault(it =>
                PriceMutations.TryGetValue(it.Key, out PriceMutation? priceMutation) &&
                priceMutation is not RemovePriceMutation);
        if (conflictingPrice != null)
        {
            throw new AmbiguousPriceException(conflictingPrice, price);
        }

        // check whether there is no conflicting update
        UpsertPriceMutation? conflictingMutation = PriceMutations.Values
            .Where(it => it is UpsertPriceMutation)
            .Cast<UpsertPriceMutation>()
            .Where(it => it.PriceKey.PriceList.Equals(price.PriceList))
            .Where(it => it.PriceKey.Currency.Equals(price.Currency))
            .Where(it => it.PriceKey.PriceId != price.PriceId)
            .Where(it => Equals(it.InnerRecordId, price.InnerRecordId))
            .FirstOrDefault(it => it.Validity == null || it.Validity is not null && price.Validity is not null &&
                it.Validity.Overlaps(price.Validity));

        if (conflictingMutation != null)
        {
            throw new AmbiguousPriceException(
                conflictingMutation.MutateLocal(
                    EntitySchema,
                    BasePrices.GetPriceWithoutSchemaCheck(conflictingMutation.PriceKey)
                ),
                price
            );
        }
    }
    
    private ICollection<IPrice> GetPricesWithoutPredicate()
    {
        List<IPrice?> prices = new List<IPrice?>();
        prices.AddRange(
            BasePrices
                .GetPrices()
                .Select(it =>
                {
                    if (PriceMutations.TryGetValue(it.Key, out PriceMutation? mutation))
                    {
                        IPrice mutatedValue = mutation.MutateLocal(EntitySchema, it);
                        return mutatedValue.DiffersFrom(it) ? mutatedValue : it;
                    }

                    return RemoveAllNonModifiedPrices ? null : it;
                })
                .Where(x=>x is not null && !x.Dropped)
            );
        
        prices.AddRange(PriceMutations.Values
            .Where(it => BasePrices.GetPriceWithoutSchemaCheck(it.PriceKey) == null)
            .Select(it => it.MutateLocal(EntitySchema, null)));
        return prices
            .Where(x => x is not null)
            .Cast<IPrice>()
            .ToList();
    }
    
    private IPrice? GetPriceInternal(PriceKey priceKey) {
        IPrice? price = BasePrices.GetPriceWithoutSchemaCheck(priceKey);

        return PriceMutations.TryGetValue(priceKey, out PriceMutation? mutation) ? mutation.MutateLocal(EntitySchema, price) : null;
    }

    public IPrice? GetPrice(PriceKey priceKey)
    {
        return GetPriceInternal(priceKey);
    }

    public IPrice? GetPrice(int priceId, string priceList, Currency currency)
    {
        return GetPriceInternal(new PriceKey(priceId, priceList, currency));
    }

    public bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode)
    {
        if (PricePredicate.Currency is not null && PricePredicate.PriceLists is not null &&
            PricePredicate.PriceLists.Any())
        {
            //return HasPriceInInterval(from, to, queryPriceMode, PricePredicate.Currency, PricePredicate.PriceLists);
            // TODO: should we compute it here? or just fetch it from the server
            return true;
        }

        throw new ContextMissingException();
    }

    public IEnumerable<IPrice> GetPrices()
    {
        return GetPricesWithoutPredicate()
            .ToList();
    }

    public bool PricesAvailable()
    {
        return BasePrices.PricesAvailable();
    }

    public IList<IPrice> GetAllPricesForSale(Currency? currency, DateTimeOffset? atTheMoment, params string[] priceListPriority)
    {
        return BasePrices.GetAllPricesForSale(currency, atTheMoment, priceListPriority);
    }

    public IList<IPrice> GetAllPricesForSale()
    {
        return BasePrices.GetAllPricesForSale();
    }

    public IPricesBuilder SetPrice(int priceId, string priceList, Currency currency, decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, bool sellable)
    {
        return SetPrice(priceId, priceList, currency, null, priceWithoutTax, taxRate, priceWithTax, null, sellable);

    }

    public IPricesBuilder SetPrice(int priceId, string priceList, Currency currency, int? innerRecordId,
        decimal priceWithoutTax, decimal taxRate, decimal priceWithTax, bool sellable)
    {
        return SetPrice(priceId, priceList, currency, innerRecordId, priceWithoutTax, taxRate, priceWithTax, null, sellable);
    }

    public IPricesBuilder SetPrice(int priceId, string priceList, Currency currency, decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, DateTimeRange? validity, bool sellable)
    {
        return SetPrice(priceId, priceList, currency, null, priceWithoutTax, taxRate, priceWithTax, validity, sellable);
    }

    public IPricesBuilder SetPrice(int priceId, string priceList, Currency currency, int? innerRecordId,
        decimal priceWithoutTax, decimal taxRate, decimal priceWithTax, DateTimeRange? validity, bool sellable)
    {
        PriceKey priceKey = new PriceKey(priceId, priceList, currency);
        UpsertPriceMutation mutation = new UpsertPriceMutation(priceKey, innerRecordId, priceWithoutTax, taxRate, priceWithTax, validity, sellable);
        AssertPriceNotAmbiguousBeforeAdding(new Price(priceKey, innerRecordId, priceWithoutTax, taxRate, priceWithTax, validity, sellable));
        PriceMutations.Add(priceKey, mutation);
        return this;
    }

    public IPricesBuilder RemovePrice(int priceId, string priceList, Currency currency)
    {
        PriceKey priceKey = new PriceKey(priceId, priceList, currency);
        Assert.NotNull(BasePrices.GetPriceWithoutSchemaCheck(priceKey), "Price " + priceKey + " doesn't exist!");
        RemovePriceMutation mutation = new RemovePriceMutation(priceKey);
        PriceMutations.Add(priceKey, mutation);
        return this;
    }

    public IPricesBuilder SetPriceInnerRecordHandling(PriceInnerRecordHandling priceInnerRecordHandling)
    {
        PriceInnerRecordHandlingEntityMutation = new SetPriceInnerRecordHandlingMutation(priceInnerRecordHandling);
        return this;
    }

    public IPricesBuilder RemovePriceInnerRecordHandling()
    {
        Assert.IsTrue(
            BasePrices.InnerRecordHandling != PriceInnerRecordHandling.None,
            "Price inner record handling is already set to `None`!"
        );
        PriceInnerRecordHandlingEntityMutation = new SetPriceInnerRecordHandlingMutation(PriceInnerRecordHandling.None);
        return this;
    }

    public IPricesBuilder RemoveAllNonTouchedPrices()
    {
        RemoveAllNonModifiedPrices = true;
        return this;
    }

    public IEnumerable<ILocalMutation> BuildChangeSet()
    {
        IList<IPrice> originalPrices =
            BasePrices.PricesAvailable() ? BasePrices.GetPrices().ToList() : new List<IPrice>();
        IDictionary<PriceKey, IPrice> builtPrices = new Dictionary<PriceKey, IPrice>();
        foreach (IPrice originalPrice in originalPrices)
        {
            builtPrices.Add(originalPrice.Key, originalPrice);
        }

        List<ILocalMutation> localMutations = new List<ILocalMutation>();
        
        if (RemoveAllNonModifiedPrices)
        {
            if (!Equals(BasePrices.InnerRecordHandling,
                    PriceInnerRecordHandlingEntityMutation?.PriceInnerRecordHandling))
            {
                localMutations.Add(PriceInnerRecordHandlingEntityMutation!);
            }

            localMutations.AddRange(PriceMutations.Values
                .Where(it =>
                {
                    IPrice? existingValue = builtPrices.TryGetValue(it.PriceKey, out IPrice? price) ? price : null;
                    IPrice newPrice = it.MutateLocal(EntitySchema, existingValue);
                    builtPrices.Add(it.PriceKey, newPrice);
                    return existingValue == null || newPrice.Version > existingValue.Version;
                }));

            localMutations.AddRange(originalPrices
                .Where(it => !PriceMutations.TryGetValue(it.Key, out _))
                .Select(it => new RemovePriceMutation(it.Key)));
        }
        else
        {
            if (!Equals(BasePrices.InnerRecordHandling,
                    PriceInnerRecordHandlingEntityMutation?.PriceInnerRecordHandling))
            {
                localMutations.Add(PriceInnerRecordHandlingEntityMutation!);
            }

            localMutations.AddRange(PriceMutations.Values
                .Where(it =>
                {
                    IPrice? existingValue = builtPrices.TryGetValue(it.PriceKey, out IPrice? price) ? price : null;
                    IPrice newPrice = it.MutateLocal(EntitySchema, existingValue);
                    builtPrices.Add(it.PriceKey, newPrice);
                    return existingValue == null || newPrice.Version > existingValue.Version;
                }));
        }

        return localMutations;
    }

    public Prices Build()
    {
        ICollection<IPrice> newPrices = GetPricesWithoutPredicate().ToList();
        IDictionary<PriceKey, IPrice> newPriceIndex = newPrices
            .ToDictionary(x => x.Key, x => x);
        PriceInnerRecordHandling? newPriceInnerRecordHandling = InnerRecordHandling;
        if (BasePrices.InnerRecordHandling != newPriceInnerRecordHandling ||
            BasePrices.GetPrices().Count() != newPrices.Count ||
            BasePrices.GetPrices()
                .Any(it => newPriceIndex.TryGetValue(it.Key, out IPrice? newPrice) && newPrice.DiffersFrom(it))
           )
        {
            return new Prices(
                BasePrices.EntitySchema,
                BasePrices.Version + 1,
                newPrices,
                newPriceInnerRecordHandling,
                newPrices.Any()
            );
        }

        return BasePrices;
    }
}