namespace Client.Queries.Requires;

public class AttributeContent : AbstractRequireConstraintLeaf, IEntityContentRequire
{
    public static readonly AttributeContent AllAttributes = new AttributeContent();
    public new bool Applicable => true;
    
    public bool AllRequested => Arguments.Length == 0;
    
    private AttributeContent(params object[] arguments) : base(arguments)
    {
    }
    
    public AttributeContent() : base()
    {
    }
    
    public AttributeContent(params string[] attributeNames) : base(attributeNames)
    {
    }
    
    public string[] GetAttributeNames()
    {
        return Arguments.Select(obj => (string) obj!).ToArray();
    }
}