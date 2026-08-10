using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Prices;

public class UpsertPriceMutation : PriceMutation
{
    public int? InnerRecordId { get; }
    public decimal PriceWithoutTax { get; }
    public decimal TaxRate { get; }
    public decimal PriceWithTax { get; }
    public DateTimeRange? Validity { get; }
    public bool Indexed { get; }
    public override Operation Operation => Operation.Upsert;

    public UpsertPriceMutation(
        int priceId,
        string priceList,
        Currency currency,
        int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax,
        DateTimeRange? validity,
        bool indexed
    ) : base(new PriceKey(priceId, priceList, currency))
    {
        InnerRecordId = innerRecordId;
        PriceWithoutTax = priceWithoutTax;
        TaxRate = taxRate;
        PriceWithTax = priceWithTax;
        Validity = validity;
        Indexed = indexed;
    }

    public UpsertPriceMutation(
        PriceKey priceKey,
        int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax,
        DateTimeRange? validity,
        bool indexed
    ) : base(priceKey)
    {
        InnerRecordId = innerRecordId;
        PriceWithoutTax = priceWithoutTax;
        TaxRate = taxRate;
        PriceWithTax = priceWithTax;
        Validity = validity;
        Indexed = indexed;
    }

    public UpsertPriceMutation(PriceKey priceKey, IPrice price) : base(priceKey)
    {
        InnerRecordId = price.InnerRecordId;
        PriceWithoutTax = price.PriceWithoutTax;
        TaxRate = price.TaxRate;
        PriceWithTax = price.PriceWithTax;
        Validity = price.Validity;
        Indexed = price.Indexed;
    }

    public override IPrice MutateLocal(IEntitySchema entitySchema, IPrice? existingValue)
    {
        if (existingValue == null) {
            return new Price(
                PriceKey,
                InnerRecordId,
                PriceWithoutTax,
                TaxRate,
                PriceWithTax,
                Validity,
                Indexed
            );
        }

        if (
            Equals(existingValue.InnerRecordId, InnerRecordId) ||
            Equals(existingValue.PriceWithoutTax, PriceWithoutTax) ||
            Equals(existingValue.TaxRate, TaxRate) ||
            Equals(existingValue.PriceWithTax, PriceWithTax) ||
            Equals(existingValue.Validity, Validity) ||
            existingValue.Indexed != Indexed
        ) {
            return new Price(
                existingValue.Key,
                InnerRecordId,
                PriceWithoutTax,
                TaxRate,
                PriceWithTax,
                Validity,
                Indexed,
                existingValue.Version + 1
            );
        }
        return existingValue;
    }
}
