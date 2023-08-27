using System.Globalization;
using Client.Models.Schemas;

namespace Client.Models.Data.Mutations.Attributes;

public class UpsertAttributeMutation : AttributeMutation
{
    public object Value { get; }

    public UpsertAttributeMutation(AttributeKey attributeKey, object value) : base(attributeKey)
    {
        Value = value;
    }

    public UpsertAttributeMutation(string attributeName, object value) : base(new AttributeKey(attributeName))
    {
        Value = value;
    }

    public UpsertAttributeMutation(string attributeName, CultureInfo locale, object value) : base(
        new AttributeKey(attributeName, locale))
    {
        Value = value;
    }

    public override AttributeValue MutateLocal(IEntitySchema entitySchema, AttributeValue? existingValue)
    {
        if (existingValue == null)
        {
            return new AttributeValue(AttributeKey, Value);
        }
        return !Equals(existingValue.Value, Value) ?
            new AttributeValue(existingValue.Version + 1, AttributeKey, Value) : existingValue;
    }
}