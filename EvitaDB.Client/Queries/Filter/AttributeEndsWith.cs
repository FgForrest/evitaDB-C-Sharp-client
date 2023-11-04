namespace EvitaDB.Client.Queries.Filter;

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