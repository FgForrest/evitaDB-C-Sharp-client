namespace Client.Queries.Filter;

public class AttributeInSet<T> : AbstractAttributeFilterConstraintLeaf
{
    private AttributeInSet(params object[] arguments) : base(arguments)
    {
    }
    
    public AttributeInSet(string attributeName, params T[] attributeValues) : base(attributeName, attributeValues)
    {
    }
    
    public object[] AttributeValues => (object[]) Arguments.Skip(1).ToArray();
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length >= 2;
}