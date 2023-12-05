namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `lessThanEquals` is query that compares value of the attribute with name passed in first argument with the value passed
/// in the second argument. First argument must be <see cref="string"/>, second argument may be any of comparable types.
/// Type of the attribute value and second argument must be convertible one to another otherwise `lessThanEquals` function
/// returns false.
/// Function returns true if value in a filterable attribute of such a name is lesser than value in second argument or
/// equal.
/// Function currently doesn't support attribute arrays and when attribute is of array type. Query returns error when this
/// query is used in combination with array type attribute. This may however change in the future.
/// Example:
/// <code>
/// lessThanEquals("age", 20)
/// </code>
/// </summary>
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
