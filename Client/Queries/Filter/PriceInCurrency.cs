using Client.DataTypes;

namespace Client.Queries.Filter;

public class PriceInCurrency : AbstractFilterConstraintLeaf
{
    public Currency Currency => Arguments[0] as Currency ?? new Currency((string) Arguments[0]!);
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    private PriceInCurrency(params object[] arguments) : base(arguments)
    {
    }
    
    public PriceInCurrency(string currency) : base(currency)
    {
    }

    public PriceInCurrency(Currency currency) : base(currency)
    {
    }
    
    
}