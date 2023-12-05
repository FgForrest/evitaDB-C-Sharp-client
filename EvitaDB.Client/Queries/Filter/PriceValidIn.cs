namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `priceValidIn` excludes all entities that don't have a valid price for sale at the specified date and time. If
/// the price doesn't have a validity property specified, it passes all validity checks.
/// Example:
/// <code>
/// priceValidIn(2020-07-30T20:37:50+00:00)
/// </code>
/// Warning: Only a single occurrence of any of this constraint is allowed in the filter part of the query.
/// Currently, there is no way to switch context between different parts of the filter and build queries such as find
/// a product whose price is either in "CZK" or "EUR" currency at this or that time using this constraint.
/// </summary>
public class PriceValidIn : AbstractFilterConstraintLeaf, IConstraintWithSuffix
{
    private const string Suffix = "now";
    public DateTimeOffset? TheMoment => Arguments.Length == 1 ? (DateTimeOffset?) Arguments[0] : null;
    public new bool Applicable => true;
    private PriceValidIn(params object[] arguments) : base(arguments)
    {
    }

    public PriceValidIn() : base()
    {
    }
    
    public PriceValidIn(DateTimeOffset theMoment) : base(theMoment)
    {
    }

    public string? SuffixIfApplied => Arguments.Length == 0 ? Suffix : null;
    bool IConstraintWithSuffix.ArgumentImplicitForSuffix(object argument) => false;
}
