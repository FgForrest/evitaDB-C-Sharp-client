namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `greaterThanEquals` is query that compares value of the attribute with name passed in first argument with the value passed
/// in the second argument. First argument must be <see cref="string"/>, second argument may be any of comparable types.
/// Type of the attribute value and second argument must be convertible one to another otherwise `greaterThanEquals` function
/// returns false.
/// Function returns true if value in a filterable attribute of such a name is greater than value in second argument or
/// equal.
/// Function currently doesn't support attribute arrays and when attribute is of array type. Query returns error when this
/// query is used in combination with array type attribute. This may however change in the future.
/// Example:
/// <code>
/// greaterThanEquals("age", 20)
/// </code>
/// </summary>
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
