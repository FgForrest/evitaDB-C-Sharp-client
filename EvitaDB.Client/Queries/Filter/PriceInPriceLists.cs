namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `priceInPriceLists` constraint defines the allowed set(s) of price lists that the entity must have to be included
/// in the result set. The order of the price lists in the argument is important for the final price for sale calculation
/// - see the <a href="https://evitadb.io/documentation/deep-dive/price-for-sale-calculation">price for sale calculation
/// algorithm documentation</a>. Price list names are represented by plain String and are case-sensitive. Price lists
/// don't have to be stored in the database as an entity, and if they are, they are not currently associated with
/// the price list code defined in the prices of other entities. The pricing structure is simple and flat for now
/// (but this may change in the future).
/// Except for the <a href="https://evitadb.io/documentation/query/filtering/price?lang=evitaql#typical-usage-of-price-constraints">standard use-case</a>
/// you can also create query with this constraint only:
/// <code>
/// priceInPriceLists(
///     "vip-group-1-level",
///     "vip-group-2-level",
///     "vip-group-3-level"
/// )
/// </code>
/// Warning: Only a single occurrence of any of this constraint is allowed in the filter part of the query.
/// Currently, there is no way to switch context between different parts of the filter and build queries such as find
/// a product whose price is either in "CZK" or "EUR" currency at this or that time using this constraint.
/// </summary>
public class PriceInPriceLists : AbstractFilterConstraintLeaf
{
    public string[] PriceLists => Arguments.Select(a => (string) a!).ToArray();
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 0;
    private PriceInPriceLists(params object?[] priceListNames) : base(priceListNames)
    {
    }
    
    public PriceInPriceLists(params string[] priceListNames) : base(priceListNames)
    {
    }
}
