using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data;

public record PriceKey : IComparable<PriceKey>
{
    public int PriceId { get; }
    public string PriceList { get; }
    public Currency Currency { get; }

    public PriceKey(int priceId, string priceList, Currency currency)
    {
        Assert.NotNull(priceList, "Price list name is mandatory value!");
        Assert.NotNull(currency, "Price currency is mandatory value!");
        PriceId = priceId;
        PriceList = priceList;
        Currency = currency;
    }

    public int CompareTo(PriceKey? other)
    {
        if (other == null)
        {
            return 1;
        }

        int result = string.Compare(Currency.CurrencyCode, other.Currency.CurrencyCode, StringComparison.Ordinal);
        if (result == 0)
        {
            result = string.Compare(PriceList, other.PriceList, StringComparison.Ordinal);
            if (result == 0)
            {
                return PriceId - other.PriceId;
            }

            return result;
        }

        return result;
    }

    public virtual bool Equals(PriceKey? other)
    {
        if (Currency.CurrencyCode != other?.Currency.CurrencyCode)
        {
            return false;
        }
        
        if (PriceList != other.PriceList)
        {
            return false;
        }
        
        return PriceId == other.PriceId;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(PriceId, PriceList, Currency.CurrencyCode);
    }
}