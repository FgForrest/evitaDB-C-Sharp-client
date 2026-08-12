namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `accompanyingPriceContent` requirement (usable inside `priceContent`-enabled entity fetch) requests
/// computation of an additional price alongside the price for sale, using the passed price list priority.
/// When no name is given the default accompanying price name is used.
/// Example:
/// <code>
/// accompanyingPriceContent("reference", "basic")
/// </code>
/// </summary>
public class AccompanyingPriceContent : AbstractRequireConstraintLeaf
{
    /// <summary>
    /// Name assigned to the accompanying price when no explicit name is requested.
    /// </summary>
    public const string DefaultAccompanyingPrice = "default";

    public string AccompanyingPriceName => (string) Arguments[0]!;

    public string[] PriceLists => Arguments.Skip(1).Cast<string>().ToArray();

    private AccompanyingPriceContent(params object?[] arguments) : base(arguments)
    {
    }

    public AccompanyingPriceContent() : base(DefaultAccompanyingPrice)
    {
    }

    public AccompanyingPriceContent(string accompanyingPriceName, params string[] priceLists) : base(
        new object?[] {accompanyingPriceName}.Concat(priceLists.Cast<object?>()).ToArray())
    {
    }
}
