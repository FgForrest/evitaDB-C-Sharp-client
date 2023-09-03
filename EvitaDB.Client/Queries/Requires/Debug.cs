namespace EvitaDB.Client.Queries.Requires;

public class Debug : AbstractRequireConstraintLeaf
{
    private Debug(params object[] arguments) : base(arguments)
    {
    }
    
    public Debug(params DebugMode[] debugModes) : base(debugModes.Cast<object>().ToArray())
    {
    }
    
    public ISet<DebugMode> DebugModes => Arguments.Where(x=>x is DebugMode).Cast<DebugMode>().ToHashSet();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length >= 1;
}