namespace Client.Queries.Order;

public class Random : AbstractOrderConstraintLeaf
{
    public new bool Applicable => true;
    private Random(params object[] arguments) : base(arguments)
    {
    }
    
    public Random() : base()
    {
    }
}