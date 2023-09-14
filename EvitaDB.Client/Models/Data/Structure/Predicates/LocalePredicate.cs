using System.Globalization;

namespace EvitaDB.Client.Models.Data.Structure.Predicates;

/// <summary>
/// This predicate allows limiting number of attributes visible to the client based on query constraints.
/// </summary>
public class LocalePredicate
{
    public static readonly LocalePredicate DefaultInstance = new(null, new HashSet<CultureInfo>());

    /// <summary>
    /// Contains information about implicitly derived locale during entity fetch.
    /// </summary>
    public CultureInfo? ImplicitLocale { get; }

    /// <summary>
    /// Contains information about all locales that has been fetched / requested for the entity.
    /// </summary>
    public ISet<CultureInfo>? Locales { get; }

    public LocalePredicate(EvitaRequestData evitaRequestData)
    {
        ImplicitLocale = evitaRequestData.ImplicitLocale;
        Locales = evitaRequestData.RequiredLocaleSet;
    }

    internal LocalePredicate(CultureInfo? implicitLocale, ISet<CultureInfo>? locales)
    {
        ImplicitLocale = implicitLocale;
        Locales = locales;
    }

    /// <summary>
    /// Test if the locale is present in the predicate.
    /// </summary>
    /// <param name="locale">locale to check</param>
    /// <returns>returns true if the locale has been requested</returns>
    public bool Check(CultureInfo locale)
    {
        return (Locales != null && (Locales.Any() || Locales.Contains(locale))) ||
               (ImplicitLocale != null && Equals(ImplicitLocale, locale));
    }
}