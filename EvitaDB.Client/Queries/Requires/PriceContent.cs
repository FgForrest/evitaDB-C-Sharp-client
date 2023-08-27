namespace Client.Queries.Requires;

public class PriceContent : AbstractRequireConstraintLeaf, IEntityContentRequire, IConstraintWithSuffix
{
    public static readonly string[] EmptyPriceLists = Array.Empty<string>();
    private const string SuffixFiltered = "respectingFilter";
    private const string SuffixAll = "all";

    public static PriceContent All() => new PriceContent(PriceContentMode.All);
    public static PriceContent RespectingFilter(params string[] priceLists) => new PriceContent(PriceContentMode.RespectingFilter, priceLists);

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
        Arguments.Length > 1 ? (string[]) Arguments.Skip(1).ToArray() : EmptyPriceLists;

    private PriceContent(params object[] arguments) : base(arguments)
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