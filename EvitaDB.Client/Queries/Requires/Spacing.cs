namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `spacing` container (used inside the `page` requirement) reserves space on result pages for non-entity
/// content (advertisements, banners etc.) by inserting <see cref="SpacingGap"/>s that shrink the page capacity
/// on pages matching the gap expressions.
/// Example:
/// <code>
/// page(1, 20, spacing(gap(2, "$pageNumber % 2 == 0")))
/// </code>
/// </summary>
public class Spacing : AbstractRequireConstraintContainer
{
    public SpacingGap[] Gaps => Children.OfType<SpacingGap>().ToArray();

    public Spacing(params SpacingGap[] gaps) : base(gaps.Cast<IRequireConstraint?>().ToArray())
    {
    }

    public new bool Necessary => Applicable;

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new Spacing(children.OfType<SpacingGap>().ToArray());
    }
}
