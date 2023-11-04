namespace EvitaDB.Client.Queries.Order;

public class AttributeSetExact : AbstractOrderConstraintLeaf
{
    private AttributeSetExact(params object?[] args) : base(args)
    {
    }

    public AttributeSetExact(string attributeName, params object[] attributeValues) 
        : base(new object[] {attributeName}.Concat(attributeValues).ToArray())
    {
    }
    
    public string AttributeName => (string) Arguments[0]!;
    
    public object[] AttributeValues => Arguments.Skip(1).ToArray()!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 1;
}