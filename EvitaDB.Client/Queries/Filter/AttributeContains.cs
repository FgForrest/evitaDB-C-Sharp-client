namespace Client.Queries.Filter;

public class AttributeContains : AbstractAttributeFilterConstraintLeaf
{
    public string TextToSearch => (string) Arguments[1]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;

    private AttributeContains(params object[] arguments) : base(arguments)
    {
    }
    
    public AttributeContains(string attributeName, string textToSearch) : base(attributeName, textToSearch)
    {
    }
}