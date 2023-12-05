namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `startsWith` is query that searches value of the attribute with name passed in first argument for presence of the
/// <see cref="string"/> value passed in the second argument.
/// Function returns true if attribute value contains secondary argument (from first position). InSet other words attribute
/// value starts with string passed in second argument. Function is case-sensitive and comparison is executed using `UTF-8`
/// encoding (C# native).
/// Example:
/// <code>
/// startsWith("code", "vid")
/// </code>
/// Function supports attribute arrays and when attribute is of array type `startsWith` returns true if any of attribute
/// values starts with the value in the query. If we have the attribute `code` with value `["cat","mouse","dog"]` all
/// these constraints will match:
/// <code>
/// contains("code","mou")
/// contains("code","do")
/// </code>
/// </summary>
public class AttributeStartsWith : AbstractAttributeFilterConstraintLeaf
{
    private AttributeStartsWith(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeStartsWith(string attributeName, string textToSearch) : base(attributeName, textToSearch)
    {
    }
    
    public string TextToSearch => (string) Arguments[1]!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;
}
