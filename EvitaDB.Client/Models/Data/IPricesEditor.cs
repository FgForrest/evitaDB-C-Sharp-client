using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Structure;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Contract for classes that allow creating / updating or removing information about prices in <see cref="ISealedEntity"/> instance.
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="IPrices"/> interface.
/// </summary>
/// <typeparam name="TW">price altering editor type</typeparam>
public interface IPricesEditor<out TW> : IPrices where TW : IPricesEditor<TW>
{
    /// <summary>
    /// Creates or updates price with key properties: priceId, priceList, currency.
    /// Beware! If priceId and currency stays the same, but only price list changes (although it's unlikely), you need
    /// <see cref="RemovePrice(int, String, Currency)"/> first and set it with new price list.
    /// </summary>
    /// <param name="priceId">identification of the price in the external systems</param>
    /// <param name="priceList">identification of the price list (either external or internal <see cref="ISealedEntity.PrimaryKey"/></param>
    /// <param name="currency">identification of the currency. Three-letter form according to <a href="https://en.wikipedia.org/wiki/ISO_4217">ISO 4217</a></param>
    /// <param name="priceWithoutTax">price without tax</param>
    /// <param name="taxRate">tax percentage (i.e. for 19% it'll be 19.00)</param>
    /// <param name="priceWithTax">price with tax</param>
    /// <param name="sellable">controls whether price is subject to filtering / sorting logic <see cref="Price.Sellable"/></param>
    /// <exception cref="AmbiguousPriceException">when there are two prices in same price list and currency which validates overlap</exception>
    /// <returns>builder instance to allow command chaining</returns>
	TW SetPrice(
		int priceId,
		string priceList,
		Currency currency,
		decimal priceWithoutTax,
		decimal taxRate,
		decimal priceWithTax,
		bool sellable
	);
    
	/// <summary>
	/// Creates or updates price with key properties: priceId, priceList, currency.
	/// Beware! If priceId and currency stays the same, but only price list changes (although it's unlikely), you need
	/// <see cref="RemovePrice(int, String, Currency)"/> first and set it with new price list.
	/// </summary>
	/// <param name="priceId">identification of the price in the external systems</param>
	/// <param name="priceList">identification of the price list (either external or internal <see cref="ISealedEntity.PrimaryKey"/></param>
	/// <param name="currency">identification of the currency. Three-letter form according to <a href="https://en.wikipedia.org/wiki/ISO_4217">ISO 4217</a></param>
	/// <param name="innerRecordId">sub-record identification <see cref="Price.InnerRecordId"/>, must be positive value</param>
	/// <param name="priceWithoutTax">price without tax</param>
	/// <param name="taxRate">tax percentage (i.e. for 19% it'll be 19.00)</param>
	/// <param name="priceWithTax">price with tax</param>
	/// <param name="sellable">controls whether price is subject to filtering / sorting logic <see cref="Price.Sellable"/></param>
	/// <exception cref="AmbiguousPriceException">when there are two prices in same price list and currency which validates overlap</exception>
	/// <returns>builder instance to allow command chaining</returns>
	TW SetPrice(
		int priceId,
		string priceList,
		Currency currency,
		int? innerRecordId,
		decimal priceWithoutTax,
		decimal taxRate,
		decimal priceWithTax,
		bool sellable
	);
	
	/// <summary>
	/// Creates or updates price with key properties: priceId, priceList, currency.
	/// Beware! If priceId and currency stays the same, but only price list changes (although it's unlikely), you need
	/// <see cref="RemovePrice(int, String, Currency)"/> first and set it with new price list.
	/// </summary>
	/// <param name="priceId">identification of the price in the external systems</param>
	/// <param name="priceList">identification of the price list (either external or internal <see cref="ISealedEntity.PrimaryKey"/></param>
	/// <param name="currency">identification of the currency. Three-letter form according to <a href="https://en.wikipedia.org/wiki/ISO_4217">ISO 4217</a></param>
	/// <param name="priceWithoutTax">price without tax</param>
	/// <param name="taxRate">tax percentage (i.e. for 19% it'll be 19.00)</param>
	/// <param name="priceWithTax">price with tax</param>
	/// <param name="validity">date and time interval for which the price is valid (inclusive)</param>
	/// <param name="sellable">controls whether price is subject to filtering / sorting logic <see cref="Price.Sellable"/></param>
	/// <exception cref="AmbiguousPriceException">when there are two prices in same price list and currency which validates overlap</exception>
	/// <returns>builder instance to allow command chaining</returns>
	TW SetPrice(
		int priceId,
		string priceList,
		Currency currency,
		decimal priceWithoutTax,
		decimal taxRate,
		decimal priceWithTax,
		DateTimeRange? validity,
		bool sellable
	);
	
	/// <summary>
	/// Creates or updates price with key properties: priceId, priceList, currency.
	/// Beware! If priceId and currency stays the same, but only price list changes (although it's unlikely), you need
	/// <see cref="RemovePrice(int, String, Currency)"/> first and set it with new price list.
	/// </summary>
	/// <param name="priceId">identification of the price in the external systems</param>
	/// <param name="priceList">identification of the price list (either external or internal <see cref="ISealedEntity.PrimaryKey"/></param>
	/// <param name="currency">identification of the currency. Three-letter form according to <a href="https://en.wikipedia.org/wiki/ISO_4217">ISO 4217</a></param>
	/// <param name="innerRecordId">sub-record identification <see cref="Price.InnerRecordId"/>, must be positive value</param>
	/// <param name="priceWithoutTax">price without tax</param>
	/// <param name="taxRate">tax percentage (i.e. for 19% it'll be 19.00)</param>
	/// <param name="priceWithTax">price with tax</param>
	/// <param name="validity">date and time interval for which the price is valid (inclusive)</param>
	/// <param name="sellable">controls whether price is subject to filtering / sorting logic <see cref="Price.Sellable"/></param>
	/// <exception cref="AmbiguousPriceException">when there are two prices in same price list and currency which validates overlap</exception>
	/// <returns>builder instance to allow command chaining</returns>
	TW SetPrice(
		int priceId,
		string priceList,
		Currency currency,
		int? innerRecordId,
		decimal priceWithoutTax,
		decimal taxRate,
		decimal priceWithTax,
		DateTimeRange? validity,
		bool sellable
	);
	
	/// <summary>
	/// Removes existing price by specifying key properties.
	/// </summary>
	/// <param name="priceId">identification of the price in the external systems</param>
	/// <param name="priceList">identification of the price list (either external or internal <see cref="ISealedEntity.PrimaryKey"/></param>
	/// <param name="currency">identification of the currency. Three-letter form according to <a href="https://en.wikipedia.org/wiki/ISO_4217">ISO 4217</a></param>
	/// <returns>builder instance to allow command chaining</returns>
	TW RemovePrice(
		int priceId,
		string priceList,
		Currency currency
	);

	/// <summary>
	/// Sets behaviour for prices that has <see cref="Price.InnerRecordId"/> set in terms of computing the "selling" price.
	/// </summary>
	/// <param name="priceInnerRecordHandling">handling mode of prices with the same <see cref="Price.InnerRecordId"/></param>
	/// <returns>builder instance to allow command chaining</returns>
	TW SetPriceInnerRecordHandling(PriceInnerRecordHandling priceInnerRecordHandling);

	/// <summary>
	/// Removes previously set behaviour for prices with <see cref="Price.InnerRecordId"/>. You should ensure that
	/// the entity has no prices with non-null <see cref="Price.InnerRecordId"/>.
	/// </summary>
	/// <returns>builder instance to allow command chaining</returns>
	TW RemovePriceInnerRecordHandling();

	/// <summary>
	/// <remarks>
	///	<para>
	/// This is helper method that allows to purge all methods, that were not overwritten
	/// (i.e. <see cref="SetPrice(int,string,EvitaDB.Client.DataTypes.Currency,int?,decimal,decimal,decimal,bool)"/>>
	/// by instance of this editor/builder class. It's handy if you know that whenever any price is updated in the entity
	/// you also update all other prices (i.e. all prices are rewritten). By using this method you don't need to care about
	/// purging the previous set of superfluous prices.
	/// </para>
	/// 
	/// <list type="bullet">
	///		<listheader>
	///			<description>This method is analogical to following process:</description>
	///		</listheader>
	///		<item>
	/// 		<description>clear all prices</description>
	/// 	</item>
	///		<item>
	/// 		<description>set them all from scratch</description>
	/// 	</item>
	/// </list>
	///
	/// <list type="bullet">
	///		<listheader>
	///			<description>Now you can simply:</description>
	///		</listheader>
	///		<item>
	/// 		<description>set all prices</description>
	/// 	</item>
	///		<item>
	/// 		<description>remove all non "touched" prices</description>
	/// 	</item>
	/// </list>
	/// <para>
	/// Even if you set the price exactly the same (i.e. in reality it doesn't change), it'll remain - because it was
	/// "touched". This mechanism is here because we want to avoid price removal and re-insert due to optimistic locking
	/// which is supported on by-price level.
	/// </para>
	/// </remarks>
	/// </summary>
	/// <returns>builder instance to allow command chaining</returns>
	TW RemoveAllNonTouchedPrices();
}