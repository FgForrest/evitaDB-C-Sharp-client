using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `segment` container arranges one consecutive part of the query result - entities (optionally narrowed by
/// the `entityHaving` filter) are sorted by the inner `orderBy` and the segment may be limited to a maximum
/// number of entities by the `limit` constraint. Segments are evaluated in the order of their declaration in
/// the enclosing <see cref="Segments"/> container.
/// Example:
/// <code>
/// segment(
///     entityHaving(attributeEquals("new", true)),
///     orderBy(attributeNatural("orderedQuantity", DESC)),
///     limit(3)
/// )
/// </code>
/// </summary>
public class Segment : AbstractOrderConstraintContainer
{
    public EntityHaving? EntityHaving => AdditionalChildren.OfType<EntityHaving>().FirstOrDefault();

    public OrderBy OrderBy => Children.OfType<OrderBy>().First();

    public SegmentLimit? Limit => Children.OfType<SegmentLimit>().FirstOrDefault();

    private Segment(object?[] arguments, IOrderConstraint?[] children, params IConstraint?[] additionalChildren)
        : base(arguments, children, additionalChildren)
    {
    }

    public Segment(OrderBy orderBy, SegmentLimit? limit = null) : this(null, orderBy, limit)
    {
    }

    public Segment(EntityHaving? entityHaving, OrderBy orderBy, SegmentLimit? limit = null) : base(
        Array.Empty<object>(),
        limit == null ? new IOrderConstraint?[] {orderBy} : new IOrderConstraint?[] {orderBy, limit},
        entityHaving == null ? Array.Empty<IConstraint>() : new IConstraint[] {entityHaving}
    )
    {
    }

    public new bool Necessary => Applicable;

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new Segment(Array.Empty<object>(), children, additionalChildren);
    }
}
