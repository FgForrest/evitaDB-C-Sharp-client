using System.Globalization;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data;

public record AttributeKey : IComparable<AttributeKey>
{
    public string AttributeName { get; }
    public CultureInfo? Locale { get; }

    public bool Localized => Locale != null;

    public AttributeKey(string attributeName, CultureInfo? locale = null)
    {
        Assert.NotNull(attributeName, "Attribute name cannot be null");
        AttributeName = attributeName;
        Locale = locale;
    }

    public int CompareTo(AttributeKey? other)
    {
        return ComparatorUtils.CompareLocale(Locale, other?.Locale,
            () => string.Compare(AttributeName, other?.AttributeName, StringComparison.Ordinal));
    }
    
    public override string ToString()
    {
        return AttributeName + (Locale == null ? "" : ":" + Locale.TwoLetterISOLanguageName);
    }
}