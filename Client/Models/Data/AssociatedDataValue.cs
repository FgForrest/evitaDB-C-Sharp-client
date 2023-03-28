namespace Client.Models.Data;

public class AssociatedDataValue : IComparable<AssociatedDataValue>
{
    public int Version { get; }
    public AssociatedDataKey Key { get; }
    public object? Value { get; }

    public AssociatedDataValue(AssociatedDataValue baseAssociatedData, object replacedValue)
    {
        Version = baseAssociatedData.Version;
        Key = baseAssociatedData.Key;
        Value = replacedValue;
    }

    private AssociatedDataValue(AssociatedDataKey associatedDataKey, object? value = null)
    {
        Version = 1;
        Key = associatedDataKey;
        Value = value;
    }

    public AssociatedDataValue(int version, AssociatedDataKey associatedDataKey, object value)
    {
        Version = version;
        Key = associatedDataKey;
        Value = value;
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
}