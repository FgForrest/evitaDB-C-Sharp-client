namespace Client.Queries.Requires;

public class Page : AbstractRequireConstraintLeaf
{
    public int Number => (int) Arguments[0]!;
    public int PageSize => (int) Arguments[1]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;
    private Page(params object[] arguments) : base(arguments)
    {
    }
    
    public Page(int? number, int? size) : base(number ?? 1, size ?? 20)
    {
    }
}