namespace Client.Queries.Filter;

public class PriceValidIn : AbstractFilterConstraintLeaf, IConstraintWithSuffix
{
    private const string Suffix = "now";
    public DateTimeOffset? TheMoment => Arguments.Length == 1 ? (DateTimeOffset?) Arguments[0] : null;
    public new bool Applicable => true;
    private PriceValidIn(params object[] arguments) : base(arguments)
    {
    }

    public PriceValidIn() : base()
    {
    }
    
    public PriceValidIn(DateTimeOffset theMoment) : base(theMoment)
    {
    }

    public string? SuffixIfApplied => Arguments.Length == 0 ? Suffix : null;
    public bool ArgumentImplicitForSuffix => false;
}