namespace EvitaDB.Client.Queries.Filter;

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