using System.Globalization;
using Client.Utils;

namespace Client.Models.Data;

public class AttributeKey : IComparable<AttributeKey>
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
        //TODO: include locales?
        return string.Compare(AttributeName, other?.AttributeName, StringComparison.Ordinal);
    }
}