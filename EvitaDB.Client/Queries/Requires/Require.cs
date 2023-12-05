namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// Requirements have no direct parallel in other database languages. They define sideway calculations, paging,
/// the amount of data fetched for each returned entity, and so on, but never affect the number or order of returned
/// entities. They also allow to compute additional calculations that relate to the returned entities, but contain
/// other contextual data - for example hierarchy data for creating menus, facet summary for parametrized filter,
/// histograms for charts, and so on.
/// Example:
/// <code>
/// require(
///     page(1, 2),
///     entityFetch()
/// )
/// </code>
/// </summary>
public class Require : AbstractRequireConstraintContainer, IRequireConstraint
{
    public new bool Necessary => Applicable;
    public Require(params IRequireConstraint?[] children) : base(children)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new Require(children);
    }
}
