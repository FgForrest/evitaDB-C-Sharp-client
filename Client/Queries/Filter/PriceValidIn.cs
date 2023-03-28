namespace Client.Queries.Filter;

public class PriceValidIn : AbstractFilterConstraintLeaf
{
    public DateTimeOffset? TheMoment => Arguments.Length == 1 ? (DateTimeOffset?) Arguments[0] : null;
    public new bool Applicable => true;
    private PriceValidIn(params object[] arguments) : base(arguments)
    {
    }

    public PriceValidIn()
    {
    }
    
    public PriceValidIn(DateTimeOffset theMoment) : base(theMoment)
    {
    }
}