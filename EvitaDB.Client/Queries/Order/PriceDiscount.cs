namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `priceDiscount` ordering constraint sorts the entities by the difference between the selling price and
/// the reference price found in the passed price lists (the discount). The optional first argument controls
/// the sort direction (descending by default).
/// Example:
/// <code>
/// priceDiscount("reference")
/// priceDiscount(ASC, "reference")
/// </code>
/// </summary>
public class PriceDiscount : AbstractOrderConstraintLeaf
{
    public OrderDirection Order => Arguments.OfType<OrderDirection>().FirstOrDefault(OrderDirection.Desc);

    public string[] InPriceLists => Arguments.OfType<string>().ToArray();

    private PriceDiscount(params object?[] arguments) : base(arguments)
    {
    }

    public PriceDiscount(params string[] priceLists) : this(OrderDirection.Desc, priceLists)
    {
    }

    public PriceDiscount(OrderDirection order, params string[] priceLists) : base(
        order == OrderDirection.Asc
            ? new object?[] {order}.Concat(priceLists.Cast<object?>()).ToArray()
            : priceLists.Cast<object?>().ToArray()
    )
    {
    }
}
