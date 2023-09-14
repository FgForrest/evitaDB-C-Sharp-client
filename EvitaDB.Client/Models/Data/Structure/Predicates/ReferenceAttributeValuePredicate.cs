using System.Globalization;
using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Models.Data.Structure.Predicates;

/// <summary>
/// This predicate allows limiting number of attributes visible to the client based on query constraints.
/// </summary>
public class ReferenceAttributeValuePredicate
{
    /// <summary>
    /// Contains information about single locale defined for the entity.
    /// </summary>
    public CultureInfo? Locale { get; }

    /// <summary>
    /// Contains information about implicitly derived locale during entity fetch.
    /// </summary>
    public CultureInfo? ImplicitLocale { get; }

    /// <summary>
    /// Contains information about all attribute names that has been requested for the entity reference.
    /// </summary>
    public AttributeRequest ReferenceAttributes { get; }

    /// <summary>
    /// Contains information about all attribute locales that has been fetched / requested for the entity.
    /// </summary>
    private ISet<CultureInfo>? Locales { get; }
    
    /// <summary>
    /// Returns true if attribute's locale has been requested.
    /// </summary>
    public bool LocaleSet => Locale != null || ImplicitLocale != null || Locales != null;

    internal ReferenceAttributeValuePredicate(
        CultureInfo? implicitLocale,
        ISet<CultureInfo>? locales,
        AttributeRequest referenceAttributes
    )
    {
        Locale = implicitLocale ?? (locales is not null && locales.Count == 1 ? locales.First() : null);
        ImplicitLocale = implicitLocale;
        Locales = locales;
        ReferenceAttributes = referenceAttributes;
    }

    /// <summary>
    /// Returns true if the attributes were fetched along with the entity.
    /// </summary>
    public bool WasFetched() => ReferenceAttributes.RequiresEntityAttributes;

    /// <summary>
    /// Returns true if the attributes in specified locale were fetched along with the entity.
    /// </summary>
    /// <param name="locale">locale to inspect</param>
    public bool WasFetched(CultureInfo locale)
    {
        return Locales != null && !Locales.Any() || Locales is not null && Locales.Contains(locale);
    }

    /// <summary>
    /// Returns true if the attribute of particular name was fetched along with the entity.
    /// </summary>
    /// <param name="attributeName">name of the attribute to inspect</param>
    public bool WasFetched(string attributeName)
    {
        return ReferenceAttributes.RequiresEntityAttributes && (!ReferenceAttributes.AttributeSet.Any() ||
                                                                ReferenceAttributes.AttributeSet
                                                                    .Contains(attributeName));
    }

    /// <summary>
    /// Returns true if the attribute of particular name was in specified locale were fetched along with the entity.
    /// </summary>
    /// <param name="attributeName">name of the attribute to inspect</param>
    /// <param name="locale">locale of the attribute to inspect</param>
    public bool WasFetched(string attributeName, CultureInfo locale)
    {
        return ReferenceAttributes.RequiresEntityAttributes && (!ReferenceAttributes.AttributeSet.Any() ||
                                                                ReferenceAttributes.AttributeSet
                                                                    .Contains(attributeName)) &&
               (Locales is not null && !Locales.Any() || Locales is not null && Locales.Contains(locale));
    }

    /// <summary>
    /// Method verifies that the requested attribute was fetched with the entity.
    /// </summary>
    /// <exception cref="ContextMissingException">thrown when reference attributes hasn't been requested</exception>
    public void CheckFetched()
    {
        if (!ReferenceAttributes.RequiresEntityAttributes)
        {
            throw ContextMissingException.ReferenceAttributeContextMissing();
        }
    }

    /// <summary>
    /// Method verifies that the requested attribute was fetched with the entity.
    /// </summary>
    /// <param name="attributeKey"></param>
    /// <exception cref="ContextMissingException">thrown when reference's attribute specified by the key was not fetched</exception>
    public void CheckFetched(AttributeKey attributeKey)
    {
        if (!(ReferenceAttributes.RequiresEntityAttributes && (!ReferenceAttributes.AttributeSet.Any() ||
                                                               ReferenceAttributes.AttributeSet.Contains(attributeKey
                                                                   .AttributeName))))
        {
            throw ContextMissingException.ReferenceAttributeContextMissing(attributeKey.AttributeName);
        }

        if (attributeKey.Localized && !(Equals(Locale, attributeKey.Locale) || Locales is not null && !Locales.Any() ||
                                        Locales is not null && Locales.Contains(attributeKey.Locale!)))
        {
            throw ContextMissingException.AttributeLocalizationContextMissing(
                attributeKey.AttributeName,
                attributeKey.Locale!,
                (Locale is null ? Enumerable.Empty<CultureInfo>() : new[] {Locale}).Concat(Locales!)
            );
        }
    }
}