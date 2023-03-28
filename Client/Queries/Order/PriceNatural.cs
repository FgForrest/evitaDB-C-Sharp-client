namespace Client.Queries.Order;

public class PriceNatural : AbstractOrderConstraintLeaf
{
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    public OrderDirection Direction => (OrderDirection) Arguments[0]!;
    private PriceNatural(params object[] args) : base(args)
    {
    }
    
    public PriceNatural() : base(OrderDirection.Asc)
    {
    }
    
    public PriceNatural(OrderDirection direction) : base(direction)
    {
    }
}