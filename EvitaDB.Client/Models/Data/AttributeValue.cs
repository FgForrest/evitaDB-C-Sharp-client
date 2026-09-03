namespace EvitaDB.Client.Models.Data;

public record AttributeValue : IComparable<AttributeValue>, IDroppable, IContentComparator<AttributeValue>
{
    public int Version { get; }
    public AttributeKey Key { get; }
    public object? Value { get; }
    public bool Dropped { get; }
    
    public AttributeValue(AttributeKey attributeKey, object value) : this(1, attributeKey, value)
    {
    }

    public AttributeValue(int version, AttributeKey attributeKey, object value, bool dropped = false)
    {
        Version = version;
        Key = attributeKey;
        Value = value;
        Dropped = dropped;
    }

    public int CompareTo(AttributeValue? other)
    {
        return Key.CompareTo(other?.Key);
    }

    public bool DiffersFrom(AttributeValue? otherAttributeValue)
    {
        if (otherAttributeValue == null) return true;
        if (!Equals(Key, otherAttributeValue.Key)) return true;
        if (!Equals(Value, otherAttributeValue.Value)) return true;
        // removal keeps the value and only raises the tombstone, so without this a removed attribute does
        // not differ from the one it removes - and reads fall back to the pre-removal value
        return Dropped != otherAttributeValue.Dropped;
    }
    
    public override string ToString()
    {
        return (Dropped ? "❌ " : "") +
               "\uD83D\uDD11 " + Key.AttributeName + " " +
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