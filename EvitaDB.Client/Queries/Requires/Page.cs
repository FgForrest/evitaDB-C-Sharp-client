using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `page` requirement controls the number and slice of entities returned in the query response. If no page
/// requirement is used in the query, the default page 1 with the default page size 20 is used. If the requested page
/// exceeds the number of available pages, a result with the first page is returned. An empty result is only returned if
/// the query returns no result at all or the page size is set to zero. By automatically returning the first page result
/// when the requested page is exceeded, we try to avoid the need to issue a secondary request to fetch the data.
/// The information about the actual returned page and data statistics can be found in the query response, which is
/// wrapped in a so-called data chunk object. In case of the page constraint, the <see cref="PaginatedList{T}"/> is used as data
/// chunk object.
/// Example:
/// <code>
/// page(1, 24)
/// </code>
/// </summary>
public class Page : AbstractRequireConstraintContainer
{
    public int Number => (int) Arguments[0]!;
    public int PageSize => (int) Arguments[1]!;

    /// <summary>
    /// Optional spacing specification that reserves space on the pages for non-entity content.
    /// </summary>
    public Spacing? Spacing => Children.OfType<Spacing>().FirstOrDefault();

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;

    private Page(object?[] arguments, params IRequireConstraint?[] children) : base(arguments, children)
    {
    }

    public Page(int? number, int? size) : base(new object?[] {number ?? 1, size ?? 20})
    {
    }

    public Page(int? number, int? size, Spacing? spacing) : base(
        new object?[] {number ?? 1, size ?? 20},
        spacing is null ? Array.Empty<IRequireConstraint>() : new IRequireConstraint[] {spacing})
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new Page(Arguments, children);
    }
}
