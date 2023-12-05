namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `priceBetween` constraint restricts the result set to items that have a price for sale within the specified price
/// range. This constraint is typically set by the user interface to allow the user to filter products by price, and
/// should be nested inside the userFilter constraint container so that it can be properly handled by the facet or
/// histogram computations.
/// Example:
/// <code>
/// priceBetween(150.25, 220.0)
/// </code>
/// Warning: Only a single occurrence of any of this constraint is allowed in the filter part of the query.
/// Currently, there is no way to switch context between different parts of the filter and build queries such as find
/// a product whose price is either in "CZK" or "EUR" currency at this or that time using this constraint.
/// </summary>
public class PriceBetween : AbstractFilterConstraintLeaf
{
    private PriceBetween(params object?[] arguments) : base(arguments)
    {
    }
    
    public PriceBetween(decimal? minPrice, decimal? maxPrice) : base(minPrice, maxPrice)
    {
    }
    
    public decimal? From => (decimal) Arguments[0]!;
    public decimal? To => (decimal) Arguments[1]!;
    public new bool Applicable => Arguments.Length == 2 && (From != null || To != null);
}
