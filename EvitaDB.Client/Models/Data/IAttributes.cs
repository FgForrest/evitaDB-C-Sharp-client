using System.Globalization;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data;

public interface IAttributes<TS> where TS : IAttributeSchema
{
    bool AttributesAvailable();
    bool AttributesAvailable(CultureInfo locale);
    bool AttributeAvailable(string attributeName);
    bool AttributeAvailable(string attributeName, CultureInfo locale);
    object? GetAttribute(string attributeName);
    object? GetAttribute(string attributeName, CultureInfo locale);
    object[]? GetAttributeArray(string attributeName);
    object[]? GetAttributeArray(string attributeName, CultureInfo locale);
    AttributeValue? GetAttributeValue(string attributeName);
    AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale);
    AttributeValue? GetAttributeValue(AttributeKey attributeKey);
    TS? GetAttributeSchema(string attributeName);
    ISet<string> GetAttributeNames();
    ISet<AttributeKey> GetAttributeKeys();
    ICollection<AttributeValue> GetAttributeValues();
    ICollection<AttributeValue> GetAttributeValues(string attributeName);
    ISet<CultureInfo> GetAttributeLocales();

    static bool AnyAttributeDifferBetween(IAttributes<TS> first, IAttributes<TS> second)
    {
        IEnumerable<AttributeValue> thisValues =
            first.AttributesAvailable() ? first.GetAttributeValues() : new List<AttributeValue>();
        IEnumerable<AttributeValue> otherValues =
            second.AttributesAvailable() ? second.GetAttributeValues() : new List<AttributeValue>();

        if (thisValues.Count() != otherValues.Count())
        {
            return true;
        }

        return thisValues
            .Any(it =>
            {
                object? thisValue = it.Value;
                AttributeKey key = it.Key;
                AttributeValue? other = second.GetAttributeValue(key.AttributeName, key.Locale);
                if (other == null)
                {
                    return true;
                }

                object? otherValue = other.Value;
                return it.Dropped != other.Dropped || QueryUtils.ValueDiffers(thisValue, otherValue);
            });
    }
}