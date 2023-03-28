using System.Globalization;

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

    public UpsertAttributeMutation(string attributeName, CultureInfo locale, object value) : base(new AttributeKey(attributeName, locale)) {
        Value = value;
    }
}