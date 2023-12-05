using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Order;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// This `useOfPrice` require query can be used to control the form of prices that will be used for computation in
/// <see cref="PriceBetween"/> filtering, and <see cref="PriceNatural"/>,
/// ordering. Also <see cref="PriceHistogram"/> is sensitive to this setting.
/// By default, end customer form of price (e.g. price with tax) is used in all above-mentioned constraints. This could
/// be changed by using this requirement query. It has single argument that can have one of the following values:
/// [WithTax] and [WithoutTax].
/// Example:
/// <code>
/// useOfPrice(WITH_TAX)
/// </code>
/// </summary>
public class PriceType : AbstractRequireConstraintLeaf
{
    public QueryPriceMode QueryPriceMode => (QueryPriceMode) Arguments[0]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    
    public PriceType(QueryPriceMode queryPriceMode) : base(queryPriceMode)
    {
    }
}
