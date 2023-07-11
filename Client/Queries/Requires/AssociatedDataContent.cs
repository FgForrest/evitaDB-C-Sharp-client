namespace Client.Queries.Requires;

public class AssociatedDataContent : AbstractRequireConstraintLeaf, IEntityContentRequire, IConstraintWithSuffix
{
    public string[] AssociatedDataNames => Arguments.Select(obj => (string) obj!).ToArray();
    private const string Suffix = "all";
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

    public string? SuffixIfApplied => AllRequested ? Suffix : null;
    public bool ArgumentImplicitForSuffix(object argument) => false;

}