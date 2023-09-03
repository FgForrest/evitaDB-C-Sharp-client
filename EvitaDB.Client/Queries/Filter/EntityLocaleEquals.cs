using System.Globalization;

namespace EvitaDB.Client.Queries.Filter;

public class EntityLocaleEquals : AbstractFilterConstraintLeaf
{
    private EntityLocaleEquals(params object[] arguments) : base(arguments)
    {
    }
    
    public EntityLocaleEquals(CultureInfo locale) : base(locale)
    {
    }
    
    public CultureInfo Locale => (Arguments[0] as CultureInfo)!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
}