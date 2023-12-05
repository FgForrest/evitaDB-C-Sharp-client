namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `contains` is query that searches value of the attribute with name passed in first argument for presence of the
/// <see cref="string"/> value passed in the second argument.
/// 
/// Function returns true if attribute value contains secondary argument (starting with any position). Function is case
/// sensitive and comparison is executed using `UTF-8` encoding (C# native).
/// Example:
/// <code>
/// contains("code", "evitaDB")
/// </code>
/// Function supports attribute arrays and when attribute is of array type `contains` returns true if any of attribute
/// values contains the value in the query. If we have the attribute `code` with value `["cat","mouse","dog"]` all these
/// constraints will match:
/// <code>
/// contains("code","mou")
/// contains("code","o")
/// </code>
/// </summary>
public class AttributeContains : AbstractAttributeFilterConstraintLeaf
{
    public string TextToSearch => (string) Arguments[1]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;

    private AttributeContains(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeContains(string attributeName, string textToSearch) : base(attributeName, textToSearch)
    {
    }
}
