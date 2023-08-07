using System.Globalization;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Data;

public interface IAttributes
{
    bool AttributesAvailable { get; }
    object? GetAttribute(string attributeName);
    object? GetAttribute(string attributeName, CultureInfo locale);
    object[]? GetAttributeArray(string attributeName);
    object[]? GetAttributeArray(string attributeName, CultureInfo locale);
    AttributeValue? GetAttributeValue(string attributeName);
    AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale);
    AttributeValue? GetAttributeValue(AttributeKey attributeKey);
    IAttributeSchema GetAttributeSchema(string attributeName);
    ISet<string> GetAttributeNames();
    ISet<AttributeKey> GetAttributeKeys();
    ICollection<AttributeValue> GetAttributeValues();
    ICollection<AttributeValue> GetAttributeValues(string attributeName);
    ISet<CultureInfo> GetAttributeLocales();
}