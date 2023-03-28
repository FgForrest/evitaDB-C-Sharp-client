namespace Client.Models.Data;

public class AttributeValue : IComparable<AttributeValue>
{
    public int Version { get; }
    public AttributeKey Key { get; }
    public object? Value { get; }

    public AttributeValue(AttributeValue baseAttribute, object replacedValue)
    {
        Version = baseAttribute.Version;
        Key = baseAttribute.Key;
        Value = replacedValue;
    }

    private AttributeValue(AttributeKey attributeKey, object? value = null)
    {
        Version = 1;
        Key = attributeKey;
        Value = value;
    }

    public AttributeValue(int version, AttributeKey attributeKey, object value)
    {
        Version = version;
        Key = attributeKey;
        Value = value;
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