namespace EvitaDB.Client.Queries.Filter;

public class AttributeStartsWith : AbstractAttributeFilterConstraintLeaf
{
    private AttributeStartsWith(params object[] arguments) : base(arguments)
    {
    }
    
    public AttributeStartsWith(string attributeName, string textToSearch) : base(attributeName, textToSearch)
    {
    }
    
    public string TextToSearch => (string) Arguments[1]!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;
}