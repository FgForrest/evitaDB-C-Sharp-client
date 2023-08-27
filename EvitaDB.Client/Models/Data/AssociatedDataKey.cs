using System.Globalization;
using Client.Utils;

namespace Client.Models.Data;

public class AssociatedDataKey : IComparable<AssociatedDataKey>
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