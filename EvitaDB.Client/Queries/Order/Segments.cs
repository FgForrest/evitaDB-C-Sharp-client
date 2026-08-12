namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `segments` container breaks the ordered query result into a sequence of consecutive segments, each with its
/// own inner ordering and optional size limit. It must be the only ordering constraint on its level when used.
/// Example:
/// <code>
/// segments(
///     segment(orderBy(attributeNatural("orderedQuantity", DESC)), limit(3)),
///     segment(orderBy(random()))
/// )
/// </code>
/// </summary>
public class Segments : AbstractOrderConstraintContainer
{
    public Segment[] SegmentList => Children.OfType<Segment>().ToArray();

    public Segments(params Segment[] segments) : base(segments.Cast<IOrderConstraint?>().ToArray())
    {
    }

    public new bool Necessary => Applicable;

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new Segments(children.OfType<Segment>().ToArray());
    }
}
