namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `endsWith` is query that searches value of the attribute with name passed in first argument for presence of the
/// <see cref="string"/> value passed in the second argument.
/// Function returns true if attribute value contains secondary argument (using reverse lookup from last position).
/// InSet other words attribute value ends with string passed in second argument. Function is case sensitive and comparison
/// is executed using `UTF-8` encoding (C# native).
/// Example:
/// <code>
/// endsWith("code", "ida")
/// </code>
/// Function supports attribute arrays and when attribute is of array type `endsWith` returns true if any of attribute
/// values ends with the value in the query. If we have the attribute `code` with value `["cat","mouse","dog"]` all these
/// constraints will match:
/// <code>
/// contains("code","at")
/// contains("code","og")
/// </code>
/// </summary>
public class AttributeEndsWith : AbstractAttributeFilterConstraintLeaf
{
    private AttributeEndsWith(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeEndsWith(string attributeName, string textToSearch) : base(attributeName, textToSearch)
    {
    }
    
    public string TextToSearch => (string) Arguments[1]!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;
}
