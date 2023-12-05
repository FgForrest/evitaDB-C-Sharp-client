using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `priceContent` requirement allows you to access the information about the prices of the entity.
/// If the <see cref="PriceContentMode.RespectingFilter"/> mode is used, the `priceContent` requirement will only retrieve
/// the prices selected by the <see cref="PriceInPriceLists"/> constraint. If the enum <see cref="PriceContentMode.None"/> is
/// specified, no prices are returned at all, if the enum <see cref="PriceContentMode.All"/> is specified, all prices of
/// the entity are returned regardless of the priceInPriceLists constraint in the filter (the constraint still controls
/// whether the entity is returned at all).
/// You can also add additional price lists to the list of price lists passed in the priceInPriceLists constraint by
/// specifying the price list names as string arguments to the `priceContent` requirement. This is useful if you want to
/// fetch non-indexed prices of the entity that cannot (and are not intended to) be used to filter the entities, but you
/// still want to fetch them to display in the UI for the user.
/// Example:
/// <code>
/// priceContentRespectingFilter()
/// priceContentRespectingFilter("reference")
/// priceContentAll()
/// </code>
/// </summary>
public class PriceContent : AbstractRequireConstraintLeaf, IEntityContentRequire, IConstraintWithSuffix
{
    public static readonly string[] EmptyPriceLists = Array.Empty<string>();
    private const string SuffixFiltered = "respectingFilter";
    private const string SuffixAll = "all";

    public static PriceContent All() => new PriceContent(PriceContentMode.All);
    public static PriceContent RespectingFilter(params string[] priceLists) => new(PriceContentMode.RespectingFilter, priceLists);

    public string? SuffixIfApplied
    {
        get
        {
            return FetchMode switch
            {
                PriceContentMode.None => null,
                PriceContentMode.RespectingFilter => SuffixFiltered,
                PriceContentMode.All => SuffixAll,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public bool ArgumentImplicitForSuffix(object argument) =>
        argument is PriceContentMode && FetchMode != PriceContentMode.None;

    public PriceContentMode FetchMode =>
        Arguments[0]! as PriceContentMode? ?? Enum.Parse<PriceContentMode>(FetchMode.ToString());

    public string[] AdditionalPriceListsToFetch =>
        (Arguments.Length > 1 ? Arguments.Skip(1).ToArray() as string[] : EmptyPriceLists)!;

    private PriceContent(params object?[] arguments) : base(arguments)
    {
    }

    public PriceContent() : this(PriceContentMode.RespectingFilter)
    {
    }

    public PriceContent(PriceContentMode fetchMode) : base(fetchMode)
    {
    }

    public PriceContent(PriceContentMode fetchMode, params string[] priceLists) : base(
        new object[] {fetchMode}.Concat(priceLists).ToArray())
    {
    }
}
