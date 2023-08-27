namespace Client.Queries.Filter;

public class AttributeEquals<T> : AbstractAttributeFilterConstraintLeaf
{
    public T AttributeValue => (T?) Arguments[1]!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;
    
    private AttributeEquals(params object[] arguments) : base(arguments)
    {
    }
    
    public AttributeEquals(string attributeName, T attributeValue) : base(attributeName, attributeValue)
    {
    }
}