namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `defaultAccompanyingPriceLists` requirement defines the price list priority used for computing all
/// accompanying prices requested by <see cref="AccompanyingPriceContent"/> that don't specify their own lists.
/// Example:
/// <code>
/// defaultAccompanyingPriceLists("reference", "basic")
/// </code>
/// </summary>
public class DefaultAccompanyingPriceLists : AbstractRequireConstraintLeaf
{
    public string[] PriceLists => Arguments.Cast<string>().ToArray();

    private DefaultAccompanyingPriceLists(params object?[] arguments) : base(arguments)
    {
    }

    public DefaultAccompanyingPriceLists(params string[] priceLists) : base(priceLists.Cast<object?>().ToArray())
    {
    }
}
