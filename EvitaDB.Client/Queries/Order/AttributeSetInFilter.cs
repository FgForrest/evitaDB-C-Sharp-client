namespace EvitaDB.Client.Queries.Order;

public class AttributeSetInFilter : AbstractOrderConstraintLeaf
{
    private AttributeSetInFilter(params object[] args) : base(args)
    {
    }
    
    public AttributeSetInFilter(string attributeName) : base(attributeName)
    {
    }
    
    public string AttributeName => (string) Arguments[0]!;
}