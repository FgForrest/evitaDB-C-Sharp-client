namespace EvitaDB.Client.Queries.Filter;

public class PriceInPriceLists : AbstractFilterConstraintLeaf
{
    public string[] PriceLists => Arguments.Select(a => (string) a!).ToArray();
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 0;
    private PriceInPriceLists(params object[] priceListNames) : base(priceListNames)
    {
    }
    
    public PriceInPriceLists(params string[] priceListNames) : base(priceListNames)
    {
    }
}