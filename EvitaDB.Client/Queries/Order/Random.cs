namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// Random ordering is useful in situations where you want to present the end user with the unique entity listing every
/// time he/she accesses it. The constraint makes the order of the entities in the result random and does not take any
/// arguments.
/// Example:
/// <code>
/// random()
/// </code>
/// </summary>
public class Random : AbstractOrderConstraintLeaf
{
    public new bool Applicable => true;
    private Random(params object?[] arguments) : base(arguments)
    {
    }
    
    public Random() : base()
    {
    }
}
