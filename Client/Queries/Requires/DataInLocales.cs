using System.Globalization;

namespace Client.Queries.Requires;

public class DataInLocales : AbstractRequireConstraintLeaf, IEntityContentRequire
{
    public CultureInfo[] Locales => Arguments.Select(obj => (CultureInfo) obj!).ToArray();
    public new bool Applicable => true;
    public bool AllRequested => Arguments.Length == 0;
    
    private DataInLocales(params object[] arguments) : base(arguments)
    {
    }
    
    public DataInLocales(params CultureInfo[] info) : base(info)
    {
    }
}