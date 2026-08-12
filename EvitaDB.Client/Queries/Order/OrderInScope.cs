using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `inScope` ordering container is used in queries targeting multiple scopes to limit a part of the ordering to
/// entities in a particular scope only.
/// Example:
/// <code>
/// inScope(LIVE, attributeNatural("orderedQuantity", DESC))
/// </code>
/// </summary>
public class OrderInScope : AbstractOrderConstraintContainer
{
    private const string ConstraintName = "inScope";

    public Scope Scope => (Scope) Arguments[0]!;

    public IOrderConstraint?[] Ordering => Children;

    private OrderInScope(object?[] arguments, params IOrderConstraint?[] children) : base(ConstraintName, arguments,
        children, Array.Empty<IConstraint>())
    {
    }

    public OrderInScope(Scope scope, params IOrderConstraint?[] ordering) : base(ConstraintName,
        new object[] {scope}, ordering, Array.Empty<IConstraint>())
    {
    }

    public new bool Necessary => Applicable;

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new OrderInScope(Scope, children);
    }
}
