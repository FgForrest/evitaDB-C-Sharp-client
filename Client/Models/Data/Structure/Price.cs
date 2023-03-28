using Client.DataTypes;
using Client.Utils;

namespace Client.Models.Data.Structure;

public class Price
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
    
    public Price(
		int version,
		PriceKey priceKey,
		int? innerRecordId,
		decimal priceWithoutTax,
		decimal taxRate,
		decimal priceWithTax,
		DateTimeRange? validity,
		bool sellable
	) {
		Assert.NotNull(priceKey, PriceKeyIsMandatoryValue);
		Assert.NotNull(priceWithoutTax, PriceWithoutTaxIsMandatoryValue);
		Assert.NotNull(taxRate, PriceTaxIsMandatoryValue);
		Assert.NotNull(priceWithTax, PriceWithTaxIsMandatoryValue);
		Assert.IsTrue(innerRecordId is null or > 0, PriceInnerRecordIdMustBePositiveValue);
		Version = version;
		Key = priceKey;
		InnerRecordId = innerRecordId;
		PriceWithoutTax = priceWithoutTax;
		TaxRate = taxRate;
		PriceWithTax = priceWithTax;
		Validity = validity;
		Sellable = sellable;
	}

	public Price(
		PriceKey priceKey,
		int? innerRecordId,
		decimal priceWithoutTax,
		decimal taxRate,
		decimal priceWithTax,
		DateTimeRange? validity,
		bool sellable
	) {
		Assert.NotNull(priceKey, PriceKeyIsMandatoryValue);
		Assert.NotNull(priceWithoutTax, PriceWithoutTaxIsMandatoryValue);
		Assert.NotNull(taxRate, PriceTaxIsMandatoryValue);
		Assert.NotNull(priceWithTax, PriceWithTaxIsMandatoryValue);
		Assert.IsTrue(InnerRecordId is null or > 0, PriceInnerRecordIdMustBePositiveValue);
		Version = 1;
		Key = priceKey;
		InnerRecordId = innerRecordId;
		PriceWithoutTax = priceWithoutTax;
		TaxRate = taxRate;
		PriceWithTax = priceWithTax;
		Validity = validity;
		Sellable = sellable;
	}
}