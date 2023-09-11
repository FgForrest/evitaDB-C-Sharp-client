using EvitaDB.Client.Models.Data;

namespace EvitaDB.Client.Exceptions;

/// <summary>
/// This exception is thrown when user tries to add another price with same price list and currency with validity
/// overlaps the validity of already existing price with the same price list and currency. In this situation the Evita
/// wouldn't be able to decide which one of these prices should be used as selling price and that's why we report this
/// situation early with this exception.
/// </summary>
public class AmbiguousPriceException : EvitaInvalidUsageException
{
    public IPrice ExistingPrice { get; }
    public IPrice AmbiguousPrice { get; }

    public AmbiguousPriceException(IPrice existingPrice, IPrice ambiguousPrice)
    : base("Price `" + ambiguousPrice.Key + "` with id `" + ambiguousPrice.PriceId + "` cannot be added to the entity. " +
           "There is already present price `" + existingPrice.Key + "` with id `" + existingPrice.PriceId + "` " +
           "that would create conflict with newly added price because their validity spans overlap.")
    {
        ExistingPrice = existingPrice;
        AmbiguousPrice = ambiguousPrice;
    }
}