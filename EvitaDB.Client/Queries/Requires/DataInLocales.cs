using System.Globalization;
using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// This `dataInLocales` query is require query that accepts zero or more <see cref="CultureInfo"/> arguments. When this
/// require query is used, result contains [entity attributes and associated data](../model/entity_model.md)
/// localized in required languages as well as global ones. If query contains no argument, global data and data
/// localized to all languages are returned. If query is not present in the query, only global attributes and
/// associated data are returned.
/// **Note:** if <see cref="EntityLocaleEquals"/> is used in the filter part of the query and `dataInLanguage`
/// require query is missing, the system implicitly uses `dataInLanguage` matching the language in filter query.
/// Only single `dataInLanguage` query can be used in the query.
/// Example that fetches only global and `en-US` localized attributes and associated data (considering there are multiple
/// language localizations):
/// <code>
/// dataInLocales("en-US")
/// </code>
/// Example that fetches all available global and localized data:
/// <code>
/// dataInLocalesAll()
/// </code>
/// </summary>
public class DataInLocales : AbstractRequireConstraintLeaf, IEntityContentRequire, IConstraintWithSuffix
{
    public CultureInfo?[] Locales => Arguments.Select(obj => (CultureInfo?) obj).ToArray();
    public new bool Applicable => true;
    public bool AllRequested => Arguments.Length == 0;
    private const string SuffixAll = "all";
    
    private DataInLocales(params object?[] arguments) : base(arguments)
    {
    }
    
    public DataInLocales(params CultureInfo[] infos) : base(infos)
    {
    }
    public string? SuffixIfApplied => AllRequested ? SuffixAll : null;
    public bool ArgumentImplicitForSuffix(object argument) => false;
}
