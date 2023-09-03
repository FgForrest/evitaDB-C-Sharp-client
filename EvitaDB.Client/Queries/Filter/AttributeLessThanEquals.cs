namespace EvitaDB.Client.Queries.Filter;

public class AttributeLessThanEquals<T> : AbstractAttributeFilterConstraintLeaf where T : IComparable
{
    private AttributeLessThanEquals(params object[] arguments) : base(arguments)
    {
    }
    
    public AttributeLessThanEquals(string attributeName, T attributeValue) : base(attributeName, attributeValue)
    {
    }
    
    public T AttributeValue => (T) Arguments[1]!;
    
    public override bool Applicable => Arguments.Length == 2 && IsArgumentsNonNull();
}