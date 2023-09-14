using System.Globalization;
using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Models.Data.Structure.Predicates;

/// <summary>
/// This predicate allows limiting number of attributes visible to the client based on query constraints.
/// </summary>
public class AttributeValuePredicate
{
    public static readonly AttributeValuePredicate DefaultInstance =
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
    /// Contains information about all attribute locales that has been fetched / requested for the entity.
    /// </summary>
    public ISet<CultureInfo>? Locales { get; }

    /// <summary>
    /// Contains information about all attribute names that has been fetched / requested for the entity.
    /// </summary>
    public ISet<string> AttributeSet { get; }

    /// <summary>
    /// Contains true if any of the attributes of the entity has been fetched / requested.
    /// </summary>
    public bool RequiresEntityAttributes { get; }

    public AttributeValuePredicate()
    {
        Locale = null;
        ImplicitLocale = null;
        Locales = null;
        AttributeSet = new HashSet<string>();
        RequiresEntityAttributes = false;
    }

    public AttributeValuePredicate(EvitaRequestData evitaRequestData)
    {
        ImplicitLocale = evitaRequestData.ImplicitLocale;
        Locales = evitaRequestData.RequiredLocaleSet;
        Locale = ImplicitLocale ?? (Locales is not null && Locales.Count == 1 ? Locales.First() : null);
        AttributeSet = evitaRequestData.EntityAttributeSet;
        RequiresEntityAttributes = evitaRequestData.EntityAttributes;
    }
    
    internal AttributeValuePredicate(
        CultureInfo? implicitLocale,
        CultureInfo? locale,
        ISet<CultureInfo>? locales,
        ISet<string> attributeSet,
        bool requiresEntityAttributes
    )
    {
        ImplicitLocale = implicitLocale;
        Locales = locales;
        Locale = locale;
        AttributeSet = attributeSet;
        RequiresEntityAttributes = requiresEntityAttributes;
    }

    /// <summary>
    /// Returns true if the attributes were fetched along with the entity.
    /// </summary>
    public bool WasFetched() => RequiresEntityAttributes;
    
    /// <summary>
    /// Returns true if the attributes in specified locale were fetched along with the entity.
    /// </summary>
    /// <param name="locale">locale to inspect</param>
    public bool WasFetched(CultureInfo locale) => Locales != null && !Locales.Any() || Locales is not null && Locales.Contains(locale);
    
    /// <summary>
    /// Returns true if the attribute of particular name was fetched along with the entity.
    /// </summary>
    /// <param name="associatedDataName">associated data name to inspect</param>
    public bool WasFetched(string associatedDataName) => RequiresEntityAttributes && (!AttributeSet.Any() || AttributeSet.Contains(associatedDataName));
    
    /// <summary>
    /// Returns true if the attribute of particular name was in specified locale were fetched along with the entity.
    /// </summary>
    /// <param name="associatedDataName">associated data name to inspect</param>
    /// <param name="locale">locale to inspect</param>
    public bool WasFetched(string associatedDataName, CultureInfo locale) => RequiresEntityAttributes && (!AttributeSet.Any() || AttributeSet.Contains(associatedDataName)) &&
                                                                             (Locales != null && !Locales.Any() || Locales is not null && Locales.Contains(locale));

    /// <summary>
    /// Method verifies that attributes was fetched with the entity.
    /// </summary>
    /// <exception cref="ContextMissingException">thrown when no attribute has been fetched</exception>
    public void CheckFetched()
    {
        if (!RequiresEntityAttributes)
        {
            throw ContextMissingException.AttributeContextMissing();
        }
    }

    /// <summary>
    /// Method verifies that the requested attribute was fetched with the entity.
    /// </summary>
    /// <param name="attributeKey">key of the attribute</param>
    /// <exception cref="ContextMissingException">thrown when associated data specified by the key was not fetched</exception>
    public void CheckFetched(AttributeKey attributeKey)
    {
        if (!(RequiresEntityAttributes &&
              (!AttributeSet.Any() || AttributeSet.Contains(attributeKey.AttributeName))))
        {
            throw ContextMissingException.AttributeContextMissing(attributeKey.AttributeName);
        }

        if (attributeKey.Localized && !(Equals(Locale, attributeKey.Locale) ||
                                        Locales != null && !Locales.Any() ||
                                        Locales is not null &&
                                        Locales.Contains(attributeKey.Locale!)))
        {
            throw ContextMissingException.AttributeLocalizationContextMissing(
                attributeKey.AttributeName,
                attributeKey.Locale!,
                (Locale == null ? Enumerable.Empty<CultureInfo>() : new[] {Locale}).Concat(Locales!).Distinct()
            );
        }
    }
}