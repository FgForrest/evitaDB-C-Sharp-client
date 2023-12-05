namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `equals` is query that compares value of the attribute with name passed in first argument with the value passed
/// in the second argument. First argument must be <see cref="string"/>, second argument may be any of comparable types.
/// Type of the attribute value and second argument must be convertible one to another otherwise `equals` function
/// returns false.
/// Function returns true if both values are equal.
/// Example:
/// <code>
/// equals("code", "abc")
/// </code>
/// Function supports attribute arrays and when attribute is of array type `equals` returns true if any of attribute values
/// equals the value in the query. If we have the attribute `code` with value `["A","B","C"]` all these constraints will
/// match:
/// <code>
/// equals("code","A")
/// equals("code","B")
/// equals("code","C")
/// </code>
/// </summary>
public class AttributeEquals<T> : AbstractAttributeFilterConstraintLeaf
{
    public T AttributeValue => (T?) Arguments[1]!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;
    
    private AttributeEquals(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeEquals(string attributeName, T attributeValue) : base(attributeName, attributeValue)
    {
    }
}
