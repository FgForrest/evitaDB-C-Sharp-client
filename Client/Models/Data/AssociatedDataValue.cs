namespace Client.Models.Data;

public class AssociatedDataValue : IComparable<AssociatedDataValue>, IDroppable
{
    public int Version { get; }
    public AssociatedDataKey Key { get; }
    public object? Value { get; }
    public bool Dropped { get; }

    public AssociatedDataValue(int version, AssociatedDataKey associatedDataKey, object? value = null,
        bool dropped = false)
    {
        Version = version;
        Key = associatedDataKey;
        Value = value;
        Dropped = dropped;
    }

    public AssociatedDataValue(int version, AssociatedDataKey associatedDataKey, object value) : this(version,
        associatedDataKey, value, false)
    {
    }

    public AssociatedDataValue(AssociatedDataKey associatedDataKey, object? value = null) : this(1, associatedDataKey,
        value, false)
    {
    }

    public int CompareTo(AssociatedDataValue? other)
    {
        return Key.CompareTo(other?.Key);
    }

    public bool DiffersFrom(AssociatedDataValue? otherAttributeValue)
    {
        if (otherAttributeValue == null) return true;
        if (!Equals(Key, otherAttributeValue.Key)) return true;
        return !Equals(Value, otherAttributeValue.Value);
    }
    
    public override string ToString()
    {
        return (Dropped ? "❌ " : "") +
               "\uD83D\uDD11 " + Key.AssociatedDataName + " " +
               (Key.Locale == null ? "" : "(" + Key.Locale.TwoLetterISOLanguageName + ")") +
               ": " +
               (
                   Value is object?[] arrayValue
                       ?
                "[" + string.Join(",", arrayValue.Where(x => x is not null).Select(x => x?.ToString())) + "]" :
                Value
            );
    }
}