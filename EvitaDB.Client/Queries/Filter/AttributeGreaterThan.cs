namespace Client.Queries.Filter;

public class AttributeGreaterThan<T> : AbstractAttributeFilterConstraintLeaf where T : IComparable
{
    private AttributeGreaterThan(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeGreaterThan(string attributeName, T value) : base(attributeName, value)
    {
    }
    
    public T AttributeValue => (T) Arguments[1]!;
    
    public override bool Applicable => Arguments.Length == 2 && IsArgumentsNonNull();
}