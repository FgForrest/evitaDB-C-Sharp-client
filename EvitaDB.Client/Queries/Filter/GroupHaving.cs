namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `groupHaving` constraint selects the group entity a surrounding constraint applies to. It wraps exactly
/// one filter constraint, which is resolved against the <b>group entity</b> domain rather than the referenced
/// entity - so `attributeEquals` inside it matches an attribute of the group, not of the reference target.
///
/// It is used, for instance, to pick one group of a grouped reference histogram:
/// <code>
/// histogramHaving(
///     "parameterValues", 50, 120,
///     groupHaving(attributeEquals("code", "height"))
/// )
/// </code>
/// </summary>
public class GroupHaving : AbstractFilterConstraintContainer
{
    private GroupHaving(IFilterConstraint?[] children) : base(children)
    {
    }

    public GroupHaving(IFilterConstraint? child) : base(child is null ? [] : [child])
    {
    }

    /// <summary>The single wrapped constraint, or null when the container is empty.</summary>
    public IFilterConstraint? Child => Children.Length == 0 ? null : Children[0];

    public new bool Necessary => Applicable;

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new GroupHaving(children);
    }
}
