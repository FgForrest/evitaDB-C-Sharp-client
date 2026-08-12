namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `limit` constraint limits the number of entities the enclosing <see cref="Segment"/> contributes to the result.
/// </summary>
public class SegmentLimit : AbstractOrderConstraintLeaf
{
    private const string ConstraintName = "limit";

    public int Limit => (int) Arguments[0]!;

    private SegmentLimit(params object?[] arguments) : base(ConstraintName, arguments)
    {
    }

    public SegmentLimit(int limit) : base(ConstraintName, limit)
    {
    }
}
