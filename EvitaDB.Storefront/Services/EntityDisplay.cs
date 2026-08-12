using System.Globalization;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Storefront.Services;

/// <summary>
/// Safe readers for entity data.
///
/// The driver is strict on purpose: <c>GetAttribute(name)</c> throws <c>ContextMissingException</c> for a
/// localized attribute and throws again if the attribute is not in the schema at all. A storefront renders
/// whatever it got and degrades gracefully, so every read here is guarded.
/// </summary>
public static class EntityDisplay
{
    /// <summary>Reads a localized attribute, falling back to the global one, then to null.</summary>
    public static string? LocalizedString(this IAttributes<IEntityAttributeSchema> entity, string attributeName,
        CultureInfo locale)
    {
        object? value = TryRead(() => entity.GetAttribute(attributeName, locale))
                        ?? TryRead(() => entity.GetAttribute(attributeName));
        return value?.ToString();
    }

    /// <summary>Reads a non-localized attribute.</summary>
    public static string? GlobalString(this IAttributes<IEntityAttributeSchema> entity, string attributeName) =>
        TryRead(() => entity.GetAttribute(attributeName))?.ToString();

    /// <summary>Display name of an entity: localized `name`, falling back to `code`, then to the primary key.</summary>
    public static string DisplayName(this ISealedEntity entity, CultureInfo locale) =>
        entity.LocalizedString(StorefrontSchema.NameAttribute, locale)
        ?? entity.GlobalString(StorefrontSchema.CodeAttribute)
        ?? $"#{entity.PrimaryKey}";

    /// <summary>
    /// Display name of anything the facet summary hands back. Facet and group entities arrive as
    /// <see cref="IEntityClassifier"/>; only when the query asked for their bodies are they sealed entities
    /// carrying attributes.
    /// </summary>
    public static string DisplayName(this IEntityClassifier classifier, CultureInfo locale) =>
        classifier is ISealedEntity entity
            ? entity.DisplayName(locale)
            : $"{classifier.Type} #{classifier.PrimaryKey}";

    /// <summary>Business code of an entity, when it has one.</summary>
    public static string? Code(this ISealedEntity entity) => entity.GlobalString(StorefrontSchema.CodeAttribute);

    /// <summary>Reads a non-localized numeric attribute, tolerating the several numeric types evitaDB uses.</summary>
    public static decimal? GlobalDecimal(this ISealedEntity entity, string attributeName)
    {
        object? value = TryRead(() => entity.GetAttribute(attributeName));
        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            short s => s,
            byte b => b,
            _ => null
        };
    }

    /// <summary>
    /// Display name of the first entity behind a reference (e.g. the product's brand), or null when the
    /// reference was not fetched or has no body.
    /// </summary>
    public static string? ReferencedName(this ISealedEntity entity, string referenceName, CultureInfo locale)
    {
        if (!entity.ReferencesAvailable())
        {
            return null;
        }
        IEnumerable<IReference> references = TryRead(() => entity.GetReferences(referenceName))
            as IEnumerable<IReference> ?? [];
        return references
            .Select(reference => reference.ReferencedEntity)
            .Where(referenced => referenced is not null)
            .Select(referenced => referenced!.DisplayName(locale))
            .FirstOrDefault();
    }

    /// <summary>
    /// Selling price for the entity, or null when the query carried no price context or nothing matched.
    /// </summary>
    public static IPrice? SellingPrice(this ISealedEntity entity)
    {
        if (!entity.PricesAvailable())
        {
            return null;
        }
        return TryRead(() => entity.PriceForSale) as IPrice;
    }

    /// <summary>
    /// The struck-through comparison price: the highest price from a different price list than the selling one.
    /// Returned only when it is actually higher, so nothing odd shows up when the profile has no reference list.
    /// </summary>
    public static IPrice? ReferencePrice(this ISealedEntity entity, IPrice? sellingPrice, bool withTax)
    {
        if (sellingPrice is null || !entity.PricesAvailable())
        {
            return null;
        }
        decimal sellingAmount = withTax ? sellingPrice.PriceWithTax : sellingPrice.PriceWithoutTax;
        return entity.GetPrices()
            .Where(price => price.PriceList != sellingPrice.PriceList
                            && price.Currency.CurrencyCode == sellingPrice.Currency.CurrencyCode)
            .Where(price => (withTax ? price.PriceWithTax : price.PriceWithoutTax) > sellingAmount)
            .MaxBy(price => withTax ? price.PriceWithTax : price.PriceWithoutTax);
    }

    /// <summary>Formats a price amount in the entity's currency for the current locale.</summary>
    public static string Format(IPrice price, bool withTax, CultureInfo locale)
    {
        decimal amount = withTax ? price.PriceWithTax : price.PriceWithoutTax;
        return Format(amount, price.Currency.CurrencyCode, locale);
    }

    public static string Format(decimal amount, string currencyCode, CultureInfo locale) =>
        string.Create(locale, $"{amount:N2} {currencyCode}");

    /// <summary>
    /// Runs a driver read that may legitimately throw for missing context or a missing attribute, and turns
    /// that into null. Used only for display reads - never to hide a transport error.
    /// </summary>
    private static object? TryRead(Func<object?> read)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
