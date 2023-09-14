using System.Globalization;
using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Models.Data.Structure.Predicates;

/// <summary>
/// This predicate allows to limit number of associated data visible to the client based on query constraints.
/// </summary>
public class AssociatedDataValuePredicate
{
    public static readonly AssociatedDataValuePredicate DefaultInstance =
        new(null, null, new HashSet<CultureInfo>(), new HashSet<string>(), true);

    /// <summary>
    /// Contains information about single locale defined for the entity.
    /// </summary>
    public CultureInfo? Locale { get; }

    /// <summary>
    /// Contains information about implicitly derived locale during entity fetch.
    /// </summary>
    public CultureInfo? ImplicitLocale { get; }

    /// <summary>
    /// Contains information about all locales of the associated data that has been fetched / requested for the entity.
    /// </summary>
    public ISet<CultureInfo>? Locales { get; }

    /// <summary>
    /// Contains information about all associated data names that has been fetched / requested for the entity.
    /// </summary>
    public ISet<string> AssociatedDataSet { get; }

    /// <summary>
    /// Contains true if any of the associated data of the entity has been fetched / requested.
    /// </summary>
    public bool RequiresEntityAssociatedData { get; }

    public AssociatedDataValuePredicate()
    {
        Locale = null;
        ImplicitLocale = null;
        Locales = null;
        AssociatedDataSet = new HashSet<string>();
        RequiresEntityAssociatedData = false;
    }

    public AssociatedDataValuePredicate(EvitaRequestData evitaRequestData)
    {
        ImplicitLocale = evitaRequestData.ImplicitLocale;
        Locales = evitaRequestData.RequiredLocaleSet;
        Locale = ImplicitLocale ?? (Locales is not null && Locales.Count == 1 ? Locales.First() : null);
        AssociatedDataSet = evitaRequestData.EntityAssociatedDataSet;
        RequiresEntityAssociatedData = evitaRequestData.EntityAssociatedData;
    }

    internal AssociatedDataValuePredicate(
        CultureInfo? implicitLocale,
        CultureInfo? locale,
        ISet<CultureInfo>? locales,
        ISet<string> associatedDataSet,
        bool requiresEntityAssociatedData
    )
    {
        ImplicitLocale = implicitLocale;
        Locales = locales;
        Locale = locale;
        AssociatedDataSet = associatedDataSet;
        RequiresEntityAssociatedData = requiresEntityAssociatedData;
    }

    /// <summary>
    /// Returns true if the associated data were fetched along with the entity.
    /// </summary>
    public bool WasFetched() => RequiresEntityAssociatedData;

    /// <summary>
    /// Returns true if the associated data in specified locale were fetched along with the entity.
    /// </summary>
    /// <param name="locale">locale to inspect</param>
    public bool WasFetched(CultureInfo locale) =>
        Locales != null && !Locales.Any() || Locales is not null && Locales.Contains(locale);

    /// <summary>
    /// Returns true if the associated data of particular name was fetched along with the entity.
    /// </summary>
    /// <param name="associatedDataName">associated data name to inspect</param>
    public bool WasFetched(string associatedDataName) => RequiresEntityAssociatedData &&
                                                         (!AssociatedDataSet.Any() ||
                                                          AssociatedDataSet.Contains(associatedDataName));

    /// <summary>
    /// Returns true if the associated data of particular name was in specified locale were fetched along with the entity.
    /// </summary>
    /// <param name="associatedDataName">associated data name to inspect</param>
    /// <param name="locale">locale to inspect</param>
    public bool WasFetched(string associatedDataName, CultureInfo locale) =>
        RequiresEntityAssociatedData && (!AssociatedDataSet.Any() || AssociatedDataSet.Contains(associatedDataName)) &&
        (Locales != null && !Locales.Any() || Locales is not null && Locales.Contains(locale));

    /// <summary>
    /// Method verifies that associated data was fetched with the entity.
    /// </summary>
    /// <exception cref="ContextMissingException">thrown when no associated data has been fetched</exception>
    public void CheckFetched()
    {
        if (!RequiresEntityAssociatedData)
        {
            throw ContextMissingException.AssociatedDataContextMissing();
        }
    }

    /// <summary>
    /// Method verifies that the requested associated data was fetched with the entity.
    /// </summary>
    /// <param name="associatedDataKey">key of the associated data</param>
    /// <exception cref="ContextMissingException">thrown when associated data specified by the key was not fetched</exception>
    public void CheckFetched(AssociatedDataKey associatedDataKey)
    {
        if (!(RequiresEntityAssociatedData &&
              (!AssociatedDataSet.Any() || AssociatedDataSet.Contains(associatedDataKey.AssociatedDataName))))
        {
            throw ContextMissingException.AssociatedDataContextMissing(associatedDataKey.AssociatedDataName);
        }

        if (associatedDataKey.Localized && !(Equals(Locale, associatedDataKey.Locale) ||
                                             Locales != null && !Locales.Any() ||
                                             Locales is not null &&
                                             Locales.Contains(associatedDataKey.Locale!)))
        {
            throw ContextMissingException.AssociatedDataLocalizationContextMissing(
                associatedDataKey.AssociatedDataName,
                associatedDataKey.Locale!,
                (Locale == null ? Enumerable.Empty<CultureInfo>() : new[] {Locale}).Concat(Locales!).Distinct()
            );
        }
    }
}