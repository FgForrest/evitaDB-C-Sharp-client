using System.Globalization;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data;

public record AssociatedDataKey : IComparable<AssociatedDataKey>
{
    public string AssociatedDataName { get; }
    public CultureInfo? Locale { get; }

    public bool Localized => Locale != null;

    public AssociatedDataKey(string associatedDataName, CultureInfo? locale = null)
    {
        Assert.NotNull(associatedDataName, "Associated data name cannot be null");
        AssociatedDataName = associatedDataName;
        Locale = locale;
    }

    public int CompareTo(AssociatedDataKey? other)
    {
        return string.Compare(AssociatedDataName, other?.AssociatedDataName, StringComparison.Ordinal);
    }
    
    public override string ToString()
    {
        return $"AssociatedDataKey[associatedDataName={AssociatedDataName}, locale={(Locale == null ? "null" : Locale.TwoLetterISOLanguageName)}]";
    }
}