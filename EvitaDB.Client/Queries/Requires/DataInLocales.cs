using System.Globalization;

namespace EvitaDB.Client.Queries.Requires;

public class DataInLocales : AbstractRequireConstraintLeaf, IEntityContentRequire, IConstraintWithSuffix
{
    public CultureInfo[] Locales => Arguments.Select(obj => (CultureInfo) obj!).ToArray();
    public new bool Applicable => true;
    public bool AllRequested => Arguments.Length == 0;
    private const string SuffixAll = "all";
    
    private DataInLocales(params object[] arguments) : base(arguments)
    {
    }
    
    public DataInLocales(params CultureInfo[] infos) : base(infos)
    {
    }
    public string? SuffixIfApplied => AllRequested ? SuffixAll : null;
    public bool ArgumentImplicitForSuffix(object argument) => false;
}