namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `inSet` is query that compares value of the attribute with name passed in first argument with all the values passed
/// in the second, third and additional arguments. First argument must be <see cref="string"/>, additional arguments may be any
/// of comparable types.
/// Type of the attribute value and additional arguments must be convertible one to another otherwise `in` function
/// skips value comparison and ultimately returns false.
/// Function returns true if attribute value is equal to at least one of additional values.
/// Example:
/// <code>
/// inSet("level", 1, 2, 3)
/// </code>
/// Function supports attribute arrays and when attribute is of array type `inSet` returns true if any of attribute values
/// equals the value in the query. If we have the attribute `code` with value `["A","B","C"]` all these constraints will
/// match:
/// <code>
/// inSet("code","A","D")
/// inSet("code","A", "B")
/// </code>
/// </summary>
public class AttributeInSet<T> : AbstractAttributeFilterConstraintLeaf
{
    private AttributeInSet(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeInSet(string attributeName, params T?[] attributeValues) : base(Concat(attributeName, attributeValues.Cast<object>().ToArray()))
    {
    }
    
    public object?[] AttributeValues => Arguments.Skip(1).ToArray();
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length >= 2;
}
