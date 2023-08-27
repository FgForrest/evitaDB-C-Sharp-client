using Client.DataTypes;
using Client.Models.Schemas;

namespace Client.Models.Data.Mutations.Price;

public class UpsertPriceMutation : PriceMutation
{
    public int? InnerRecordId { get; }
    public decimal PriceWithoutTax { get; }
    public decimal TaxRate { get; }
    public decimal PriceWithTax { get; }
    public DateTimeRange? Validity { get; }
    public bool Sellable { get; }

    public UpsertPriceMutation(
        int priceId,
        string priceList,
        Currency currency,
        int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax,
        DateTimeRange? validity,
        bool sellable
    ) : base(new PriceKey(priceId, priceList, currency))
    {
        InnerRecordId = innerRecordId;
        PriceWithoutTax = priceWithoutTax;
        TaxRate = taxRate;
        PriceWithTax = priceWithTax;
        Validity = validity;
        Sellable = sellable;
    }

    public UpsertPriceMutation(
        PriceKey priceKey,
        int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax,
        DateTimeRange? validity,
        bool sellable
    ) : base(priceKey)
    {
        InnerRecordId = innerRecordId;
        PriceWithoutTax = priceWithoutTax;
        TaxRate = taxRate;
        PriceWithTax = priceWithTax;
        Validity = validity;
        Sellable = sellable;
    }

    public UpsertPriceMutation(PriceKey priceKey, IPrice price) : base(priceKey)
    {
        InnerRecordId = price.InnerRecordId;
        PriceWithoutTax = price.PriceWithoutTax;
        TaxRate = price.TaxRate;
        PriceWithTax = price.PriceWithTax;
        Validity = price.Validity;
        Sellable = price.Sellable;
    }

    public override IPrice MutateLocal(IEntitySchema entitySchema, IPrice? existingValue)
    {
        if (existingValue == null) {
            return new Structure.Price(
                PriceKey,
                InnerRecordId,
                PriceWithoutTax,
                TaxRate,
                PriceWithTax,
                Validity,
                Sellable
            );
        }

        if (
            Equals(existingValue.InnerRecordId, InnerRecordId) ||
            Equals(existingValue.PriceWithoutTax, PriceWithoutTax) ||
            Equals(existingValue.TaxRate, TaxRate) ||
            Equals(existingValue.PriceWithTax, PriceWithTax) ||
            Equals(existingValue.Validity, Validity) ||
            existingValue.Sellable != Sellable
        ) {
            return new Structure.Price(
                existingValue.Key,
                InnerRecordId,
                PriceWithoutTax,
                TaxRate,
                PriceWithTax,
                Validity,
                Sellable,
                existingValue.Version + 1
            );
        }
        return existingValue;
    }
}