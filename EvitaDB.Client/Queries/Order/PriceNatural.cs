using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `priceNatural` constraint allows output entities to be sorted by their selling price in their natural numeric
/// order. It requires only the order direction and the price constraints in the `filterBy` section of the query.
/// The price variant (with or without tax) is determined by the <see cref="PriceType"/> requirement of the query (price with
/// tax is used by default).
/// Please read the <a href="https://evitadb.io/documentation/deep-dive/price-for-sale-calculation">price for sale
/// calculation algorithm documentation</a> to understand how the price for sale is calculated.
/// Example:
/// <code>
/// priceNatural()
/// priceNatural(Desc)
/// </code>
/// </summary>
public class PriceNatural : AbstractOrderConstraintLeaf
{
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    public OrderDirection Direction => (OrderDirection) Arguments[0]!;
    private PriceNatural(params object?[] args) : base(args)
    {
    }
    
    public PriceNatural() : base(OrderDirection.Asc)
    {
    }
    
    public PriceNatural(OrderDirection direction) : base(direction)
    {
    }
}
