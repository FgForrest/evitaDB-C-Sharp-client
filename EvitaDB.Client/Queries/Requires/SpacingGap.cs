using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `gap` constraint (used inside <see cref="Spacing"/>) inserts a gap of the given size on every page for which
/// the server-evaluated `onPage` expression yields true (e.g. <c>$pageNumber % 2 == 0</c> for even pages).
/// Example:
/// <code>
/// gap(2, "$pageNumber % 2 == 0")
/// </code>
/// </summary>
public class SpacingGap : AbstractRequireConstraintLeaf
{
    private const string ConstraintName = "gap";

    public int Size => (int) Arguments[0]!;

    public Expression OnPage => (Expression) Arguments[1]!;

    private SpacingGap(params object?[] arguments) : base(ConstraintName, arguments)
    {
    }

    public SpacingGap(int size, Expression onPage) : base(ConstraintName, size, onPage)
    {
    }

    public SpacingGap(int size, string onPage) : this(size, new Expression(onPage))
    {
    }
}
