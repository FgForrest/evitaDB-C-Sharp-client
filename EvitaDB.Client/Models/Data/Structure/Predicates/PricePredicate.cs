using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure.Predicates;

/// <summary>
/// This predicate allows to limit number of prices visible to the client based on query constraints.
/// </summary>
public class PricePredicate
{
    public static readonly PricePredicate DefaultInstance = new();
    /// <summary>
    /// Contains information about price content mode used when entity prices were fetched.
    /// </summary>
    public PriceContentMode PriceContentMode { get; }

    /// <summary>
    /// Contains information about price currency used when entity prices were fetched.
    /// </summary>
    public Currency? Currency { get; }

    /// <summary>
    /// Contains information about price validity moment used when entity prices were fetched.
    /// </summary>
    public DateTimeOffset? ValidIn { get; }

    /// <summary>
    /// Contains information about price lists used when entity prices were fetched.
    /// </summary>
    public string[]? PriceLists { get; }

    /// <summary>
    /// Contains information about price lists that should be fetched in addition to filtered prices.
    /// </summary>
    public string[]? AdditionalPriceLists { get; }

    /// <summary>
    /// Contains the same information as {@link #priceLists} but in the form of the set for faster lookups.
    /// </summary>
    public ISet<String> PriceListsAsSet { get; }

    private bool ContextAvailable { get; set; }

    public PricePredicate()
    {
        PriceContentMode = PriceContentMode.All;
        Currency = null;
        ValidIn = null;
        PriceLists = null;
        AdditionalPriceLists = null;
        PriceListsAsSet = new HashSet<string>();
        ContextAvailable = false;
    }

    public PricePredicate(EvitaRequest evitaRequest, bool? contextAvailable)
    {
        PriceContentMode = evitaRequest.GetRequiresEntityPrices();
        Currency = evitaRequest.GetRequiresCurrency();
        ValidIn = evitaRequest.GetRequiresPriceValidIn();
        PriceLists = evitaRequest.GetRequiresPriceLists();
        AdditionalPriceLists = evitaRequest.GetFetchesAdditionalPriceLists();
        PriceListsAsSet = new HashSet<string>(PriceLists.Length + AdditionalPriceLists.Length);
        foreach (var priceList in PriceLists)
        {
            PriceListsAsSet.Add(priceList);
        }

        foreach (var priceList in AdditionalPriceLists)
        {
            PriceListsAsSet.Add(priceList);
        }

        ContextAvailable = contextAvailable ?? Currency != null && !ArrayUtils.IsEmpty(PriceLists);
    }

    internal PricePredicate(
        PriceContentMode priceContentMode,
        Currency? currency,
        DateTimeOffset? validIn,
        string[]? priceLists,
        string[]? additionalPriceLists,
        ISet<string> priceListsAsSet,
        bool contextAvailable
    )
    {
        PriceContentMode = priceContentMode;
        Currency = currency;
        ValidIn = validIn;
        PriceLists = priceLists;
        AdditionalPriceLists = additionalPriceLists;
        PriceListsAsSet = priceListsAsSet;
        ContextAvailable = contextAvailable;
    }

    public void CheckFetched(Currency? currency, params string[] priceList)
    {
        switch (PriceContentMode)
        {
            case PriceContentMode.None:
                throw ContextMissingException.PricesNotFetched();
            case PriceContentMode.RespectingFilter:
                if (Currency != null && currency != null && !Equals(Currency, currency))
                {
                    throw ContextMissingException.PricesNotFetched(currency, Currency);
                }

                if (PriceListsAsSet.Any())
                {
                    foreach (string checkedPriceList in priceList)
                    {
                        if (!PriceListsAsSet.Contains(checkedPriceList))
                        {
                            throw ContextMissingException.PricesNotFetched(checkedPriceList, PriceListsAsSet);
                        }
                    }
                }
                break;
            case PriceContentMode.All:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool Test(IPrice price)
    {
        return PriceContentMode switch
        {
            PriceContentMode.All => !price.Dropped,
            PriceContentMode.None => false,
            PriceContentMode.RespectingFilter => !price.Dropped &&
                                                 (Currency == null || Equals(Currency, price.Currency)) &&
                                                 (!PriceListsAsSet.Any() ||
                                                  PriceListsAsSet.Contains(price.PriceList)) &&
                                                 (ValidIn is null || (price.Validity?.ValidFor(ValidIn.Value) ?? true)),
            _ => false
        };
    }

    public bool Fetched() => PriceContentMode != PriceContentMode.None;

    public void CheckPricesFetched()
    {
        if (PriceContentMode == PriceContentMode.None)
        {
            throw ContextMissingException.PricesNotFetched();
        }
    }
}