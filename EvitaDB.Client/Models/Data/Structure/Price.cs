using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

public class Price : IPrice
{
    private const string PriceKeyIsMandatoryValue = "Price key is mandatory value!";
    private const string PriceWithoutTaxIsMandatoryValue = "Price without tax is mandatory value!";
    private const string PriceTaxIsMandatoryValue = "Price tax is mandatory value!";
    private const string PriceWithTaxIsMandatoryValue = "Price with tax is mandatory value!";
    private const string PriceInnerRecordIdMustBePositiveValue = "Price inner record id must be positive value!";

    public int Version { get; }
    public PriceKey Key { get; }
    public int? InnerRecordId { get; }
    public decimal PriceWithoutTax { get; }
    public decimal TaxRate { get; }
    public decimal PriceWithTax { get; }
    public DateTimeRange? Validity { get; }
    public bool Sellable { get; }
    public Currency Currency => Key.Currency;
    public string PriceList => Key.PriceList;
    public int PriceId => Key.PriceId;

    public bool Dropped { get; }

    public Price(
        PriceKey priceKey,
        int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax,
        DateTimeRange? validity,
        bool sellable,
        int version = 1
    ) : this(version, priceKey, innerRecordId, priceWithoutTax, taxRate, priceWithTax, validity, sellable, false)
    {
    }
    
    public Price(
        int version,
        PriceKey priceKey,
        int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax,
        DateTimeRange? validity,
        bool sellable,
        bool dropped
    )
    {
        Assert.NotNull(priceKey, PriceKeyIsMandatoryValue);
        Assert.NotNull(priceWithoutTax, PriceWithoutTaxIsMandatoryValue);
        Assert.NotNull(taxRate, PriceTaxIsMandatoryValue);
        Assert.NotNull(priceWithTax, PriceWithTaxIsMandatoryValue);
        Assert.IsTrue(InnerRecordId is null or > 0, PriceInnerRecordIdMustBePositiveValue);
        Version = version;
        Key = priceKey;
        InnerRecordId = innerRecordId;
        PriceWithoutTax = priceWithoutTax;
        TaxRate = taxRate;
        PriceWithTax = priceWithTax;
        Validity = validity;
        Sellable = sellable;
        Dropped = dropped;
    }

    public override string ToString()
    {
        return (Dropped ? "❌ " : "") +
               "\uD83D\uDCB0 " + (Sellable ? "\uD83D\uDCB5 " : "") + PriceWithTax + " " + Key.Currency + " (" + TaxRate + "%)" +
               ", price list " + Key.PriceList +
               (Validity == null ? "" : ", valid in " + Validity) +
               ", external id " + Key.PriceId +
               (InnerRecordId == null ? "" : "/" + InnerRecordId);
    }
}