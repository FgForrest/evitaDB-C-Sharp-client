using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Models.Data;

public interface IPrices : IVersioned
{
    /// <summary>
    /// Returns price inner record handling that controls how prices that share same `inner entity id` will behave during
    /// filtering and sorting.
    /// </summary>
    PriceInnerRecordHandling? InnerRecordHandling { get; }
    /// <summary>
    /// Returns a price for which the entity should be sold. This method can be used only in context of a <see cref="Query"/>
    /// with price related constraints so that `currency` and `priceList` priority can be extracted from the query.
    /// The moment is either extracted from the query as well (if present) or current date and time is used.
    /// </summary>
    public IPrice? PriceForSale { get; }
    /// <summary>
    /// Returns price by its business key identification.
    /// </summary>
    IPrice? GetPrice(PriceKey priceKey);
    /// <summary>
    /// Returns price by its business key identification.
    /// </summary>
    IPrice? GetPrice(int priceId, string priceList, Currency currency);
    /// <summary>
    /// Returns a price for which the entity should be sold. Only indexed prices in requested currency, valid
    /// at the passed moment are taken into an account. Prices are also limited by the passed set of price lists and
    /// the first price found in the order of the requested price list ids will be returned.
    /// </summary>
    bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode);
    /// <summary>
    /// Returns all prices of the entity.
    /// </summary>
    IList<IPrice> GetPrices();
    /// <summary>
    /// Returns true if entity prices were fetched along with the entity. Calling this method before calling any
    /// other method that requires prices to be fetched will allow you to avoid {@link ContextMissingException}.
    /// 
    /// Method also returns false if the prices are not enabled for the entity by the schema. Checking this method
    /// also allows you to avoid getting an exception in such case.
    /// </summary>
    /// <returns></returns>
    bool PricesAvailable();
    /// <summary>
    /// Returns all prices for which the entity could be sold. This method can be used in context of a <see cref="Query"/>
    /// with price related constraints so that `currency` and `priceList` priority can be extracted from the query.
    /// The moment is either extracted from the query as well (if present) or current date and time is used.
    /// 
    /// The method differs from <see cref="PriceForSale"/> in the sense of never returning {@link ContextMissingException}
    /// and returning list of all possibly matching selling prices (not only single one). Returned list may be also
    /// empty if there is no such price.
    /// </summary>
    IList<IPrice> GetAllPricesForSale(Currency? currency, DateTimeOffset? atTheMoment,
        params string[] priceListPriority);

    /// <summary>
    /// Returns all prices for which the entity could be sold. This method can be used in context of a <see cref="Query"/>
    /// with price related constraints so that `currency` and `priceList` priority can be extracted from the query.
    /// The moment is either extracted from the query as well (if present) or current date and time is used.
    /// 
    /// The method differs from <see cref="PriceForSale"/> in the sense of never returning <see cref="ContextMissingException"/>
    /// and returning list of all possibly matching selling prices (not only single one). Returned list may be also
    /// empty if there is no such price.
    /// </summary>
    IList<IPrice> GetAllPricesForSale();

    /// <summary>
    /// Returns true if single price differs between first and second instance.
    /// </summary>
    public static bool AnyPriceDifferBetween(IPrices first, IPrices second)
    {
        IEnumerable<IPrice> thisValues = first.PricesAvailable() ? first.GetPrices() : new List<IPrice>();
        IEnumerable<IPrice> otherValues = second.PricesAvailable() ? second.GetPrices() : new List<IPrice>();

        var enumerable = thisValues.ToList();
        if (enumerable.Count != otherValues.Count())
        {
            return true;
        }

        return enumerable
            .Any(it => it.DiffersFrom(second.GetPrice(it.PriceId, it.PriceList, it.Currency)));
    }
}
