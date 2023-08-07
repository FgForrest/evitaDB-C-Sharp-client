namespace Client.Models.Data;

public class ReferenceKeyWithAttributeKey : IComparable<ReferenceKeyWithAttributeKey>
{
    private ReferenceKey ReferenceKey { get; }
    private AttributeKey AttributeKey { get; }
    
    public ReferenceKeyWithAttributeKey(ReferenceKey referenceKey, AttributeKey attributeKey)
    {
        ReferenceKey = referenceKey;
        AttributeKey = attributeKey;
    }
    
    public int CompareTo(ReferenceKeyWithAttributeKey? o)
    {
        int entityReferenceComparison = ReferenceKey.CompareTo(o.ReferenceKey);
        return entityReferenceComparison == 0 ? AttributeKey.CompareTo(o.AttributeKey) : entityReferenceComparison;
    }

    public override bool Equals(object? o)
    {
        if (this == o) return true;
        if (o == null || GetType() != o.GetType()) return false;
        ReferenceKeyWithAttributeKey that = (ReferenceKeyWithAttributeKey) o;
        return Equals(ReferenceKey, that.ReferenceKey) && Equals(AttributeKey, that.AttributeKey);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReferenceKey, AttributeKey);
    }
}