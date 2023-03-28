namespace Client.Queries.Requires;

public class AssociatedDataContent : AbstractRequireConstraintLeaf, IEntityContentRequire
{
    public string[] AssociatedDataNames => Arguments.Select(obj => (string) obj!).ToArray();
    public bool AllRequested => AssociatedDataNames.Length == 0;
    
    private AssociatedDataContent(params object[] arguments) : base(arguments)
    {
    }

    public AssociatedDataContent() : base()
    {
    }
    
    public AssociatedDataContent(params string[] associatedDataNames) : base(associatedDataNames)
    {
    }
}