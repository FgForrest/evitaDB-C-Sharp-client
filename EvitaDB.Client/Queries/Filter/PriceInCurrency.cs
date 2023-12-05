using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `priceInCurrency` constraint can be used to limit the result set to entities that have a price in the specified
/// currency. Except for the <a href="https://evitadb.io/documentation/query/filtering/price?lang=evitaql#typical-usage-of-price-constraints">standard use-case</a>
/// you can also create query with this constraint only:
/// <code>
/// priceInCurrency("EUR")
/// </code>
/// Warning: Only a single occurrence of any of this constraint is allowed in the filter part of the query.
/// Currently, there is no way to switch context between different parts of the filter and build queries such as find
/// a product whose price is either in "CZK" or "EUR" currency at this or that time using this constraint.
/// </summary>
public class PriceInCurrency : AbstractFilterConstraintLeaf
{
    public Currency Currency => Arguments[0] as Currency ?? new Currency((string) Arguments[0]!);
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    private PriceInCurrency(params object[] arguments) : base(null, arguments)
    {
    }

    public PriceInCurrency(string currency) : base(null, currency)
    {
    }

    public PriceInCurrency(Currency currency) : base(currency)
    {
    }
}
