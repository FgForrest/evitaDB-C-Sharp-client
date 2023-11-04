namespace EvitaDB.Client.Queries.Order;

public class AttributeNatural : AbstractOrderConstraintLeaf
{
    public string AttributeName => (string) Arguments[0]!;
    public OrderDirection Direction => (OrderDirection) Arguments[1]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;
    private AttributeNatural(params object?[] args) : base(args)
    {
    }

    public AttributeNatural(string attributeName) : base(attributeName, OrderDirection.Asc)
    {
    }
    
    public AttributeNatural(string attributeName, OrderDirection direction) : base(attributeName, direction)
    {
    }
}