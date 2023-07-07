namespace Client.Queries.Filter;

public class AttributeGreaterThanEquals<T> : AbstractAttributeFilterConstraintLeaf where T : IComparable
{
    private AttributeGreaterThanEquals(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeGreaterThanEquals(string attributeName, T value) : base(attributeName, value)
    {
    }
    
    public T AttributeValue => (T) Arguments[1]!;
    
    public override bool Applicable => Arguments.Length == 2 && IsArgumentsNonNull();
}