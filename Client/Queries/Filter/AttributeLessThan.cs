namespace Client.Queries.Filter;

public class AttributeLessThan<T> : AbstractAttributeFilterConstraintLeaf where T : IComparable
{
    private AttributeLessThan(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeLessThan(string attributeName, T value) : base(attributeName, value)
    {
    }
    
    public T AttributeValue => (T) Arguments[1]!;
    
    public override bool Applicable => Arguments.Length == 2 && IsArgumentsNonNull();
}