namespace Client.Models.Data;

public class AttributeValue : IComparable<AttributeValue>, IDroppable
{
    public int Version { get; }
    public AttributeKey Key { get; }
    public object? Value { get; }
    public bool Dropped { get; }

    public AttributeValue(AttributeValue baseAttribute, object replacedValue) : this(baseAttribute.Version,
        baseAttribute.Key, replacedValue, baseAttribute.Dropped)
    {
    }

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
        return !Equals(Value, otherAttributeValue.Value);
    }
}