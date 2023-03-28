namespace Client.Queries.Requires;

public class PriceContent : AbstractRequireConstraintLeaf, IEntityContentRequire
{
    public static readonly string[] EmptyPriceLists = Array.Empty<string>();

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
        new object[] {fetchMode}.Concat(priceLists))
    {
    }
}