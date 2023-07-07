namespace Client.Queries.Requires;

public class PriceType : AbstractRequireConstraintLeaf
{
    public QueryPriceMode QueryPriceMode => (QueryPriceMode) Arguments[0]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    
    public PriceType(QueryPriceMode queryPriceMode) : base(queryPriceMode)
    {
    }
}