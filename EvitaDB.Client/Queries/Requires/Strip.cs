namespace Client.Queries.Requires;

public class Strip : AbstractRequireConstraintLeaf
{
    public int Offset => (int) Arguments[0]!;
    public int Limit => (int) Arguments[1]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 1;
    private Strip(params object[] arguments) : base(arguments)
    {
    }
    
    public Strip(int? offset, int? limit) : base(offset ?? 0, limit ?? 20)
    {
    }
}