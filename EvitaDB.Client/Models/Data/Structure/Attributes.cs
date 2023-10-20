using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;
using Newtonsoft.Json;

namespace EvitaDB.Client.Models.Data.Structure;

public abstract class Attributes<TS> : IAttributes<TS> where TS : IAttributeSchema
{
    [JsonIgnore] protected internal IEntitySchema EntitySchema { get; }
    internal Dictionary<AttributeKey, AttributeValue> AttributeValues { get; }
    [JsonIgnore] public IDictionary<string, TS> AttributeTypes { get; }
    private ISet<string>? AttributeNames { get; set; }
    private ISet<CultureInfo>? AttributeLocales { get; set; }
    public bool Empty => AttributeValues.Count == 0;
    public bool AttributesAvailable() => true;
    public bool AttributesAvailable(CultureInfo locale) => true;
    public bool AttributeAvailable(string attributeName) => true;
    public bool AttributeAvailable(string attributeName, CultureInfo locale) => true;

    protected Attributes(
        IEntitySchema entitySchema,
        ICollection<AttributeValue> attributeValues,
        IDictionary<string, TS> attributeTypes
    )
    {
        EntitySchema = entitySchema;
        AttributeValues = attributeValues.ToDictionary(x => x.Key, x => x);
        AttributeTypes = attributeTypes;
        AttributeLocales = attributeValues
            .Where(x=>!x.Dropped)
            .Select(x=>x.Key.Locale)
            .Where(x=>x is not null)
            .ToHashSet()!;
    }

    public object? GetAttribute(string attributeName)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out TS? attributeSchema))
        {
            if (attributeSchema is null)
            {
                CreateAttributeNotFoundException(attributeName);
            }
        }

        Assert.IsTrue(!attributeSchema!.Localized,
            () => ContextMissingException.LocaleForAttributeContextMissing(attributeName));
        return AttributeValues.TryGetValue(new AttributeKey(attributeName), out AttributeValue? attributeValue)
            ? attributeValue.Value
            : null;
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out TS? attributeSchema))
        {
            if (attributeSchema is null)
            {
                CreateAttributeNotFoundException(attributeName);
            }
        }

        Assert.IsTrue(!attributeSchema!.Localized,
            () => ContextMissingException.LocaleForAttributeContextMissing(attributeName));
        if (AttributeValues.TryGetValue(new AttributeKey(attributeName), out AttributeValue? attributeValue))
        {
            if (attributeValue.Value is object[] objArray)
            {
                return objArray;
            }
            if (attributeValue.Value is Array array && array.GetType().GetElementType()!.IsValueType)
            {
                int length = array.Length;
                object[] objectArray = new object[length];

                for (int i = 0; i < length; i++)
                {
                    objectArray[i] = array.GetValue(i)!;
                }

                return objectArray;
            }
        }

        return null;
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out TS? attributeSchema))
        {
            if (attributeSchema is null)
            {
                CreateAttributeNotFoundException(attributeName);
            }
        }

        return attributeSchema!.Localized ? null :
            AttributeValues.TryGetValue(new AttributeKey(attributeName), out AttributeValue? attributeValue) ? attributeValue :
            null;
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out TS? attributeSchema))
        {
            if (attributeSchema is null)
            {
                CreateAttributeNotFoundException(attributeName);
            }
        }

        AttributeKey attributeKey = attributeSchema!.Localized
            ? new AttributeKey(attributeName, locale)
            : new AttributeKey(attributeName);
        
        return AttributeValues.TryGetValue(attributeKey, out AttributeValue? attributeValue)
            ? attributeValue.Value
            : null;
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out TS? attributeSchema))
        {
            if (attributeSchema is null)
            {
                CreateAttributeNotFoundException(attributeName);
            }
        }

        AttributeKey attributeKey = attributeSchema!.Localized
            ? new AttributeKey(attributeName, locale)
            : new AttributeKey(attributeName);
        return AttributeValues.TryGetValue(attributeKey, out AttributeValue? attributeValue)
            ? attributeValue.Value as object[]
            : null;
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out TS? attributeSchema))
        {
            CreateAttributeNotFoundException(attributeName);
        }

        AttributeKey attributeKey = attributeSchema!.Localized
            ? new AttributeKey(attributeName, locale)
            : new AttributeKey(attributeName);
        return AttributeValues.TryGetValue(attributeKey, out AttributeValue? attributeValue) ? attributeValue : null;
    }

    public TS? GetAttributeSchema(string attributeName)
    {
        return AttributeTypes.TryGetValue(attributeName, out TS? attributeSchema) ? attributeSchema : default;
    }

    public ISet<string> GetAttributeNames()
    {
        return AttributeNames ??= AttributeValues.Keys
            .Select(x => x.AttributeName)
            .ToHashSet();
    }

    public ISet<AttributeKey> GetAttributeKeys()
    {
        return AttributeValues.Keys.ToHashSet();
    }

    public ICollection<AttributeValue> GetAttributeValues()
    {
        return AttributeValues.Values.OrderByDescending(x=>x.Key.Locale?.TwoLetterISOLanguageName).ThenBy(x=>x.Key.AttributeName).ToList();
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        return GetAttributeValues()
            .Where(it => it.Key.AttributeName == attributeName)
            .ToList();
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        return AttributeLocales ??= AttributeValues.Values
            .Select(x => x.Key.Locale)
            .Where(x => x is not null)
            .ToHashSet()!;
    }

    public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
    {
        string attributeName = attributeKey.AttributeName;
        if (!AttributeTypes.TryGetValue(attributeName, out TS? attributeSchema))
        {
            if (attributeSchema is null)
            {
                CreateAttributeNotFoundException(attributeName);
            }
        }

        AttributeKey attributeKeyToUse = attributeSchema!.Localized
            ? attributeKey
            : attributeKey.Localized ? new AttributeKey(attributeName) : attributeKey;
        return AttributeValues.TryGetValue(attributeKeyToUse, out AttributeValue? attributeValue) ? attributeValue : null;
    }
    
    internal AttributeValue? GetAttributeValueWithoutSchemaCheck(AttributeKey attributeKey) {
        if (AttributeValues.TryGetValue(attributeKey, out AttributeValue? attributeValue))
        {
            return attributeValue;
        }

        if (attributeKey.Localized && AttributeValues.TryGetValue(new AttributeKey(attributeKey.AttributeName), out AttributeValue? globalAttributeValue))
        {
            return globalAttributeValue;
        }
        return null;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        
        if (ReferenceEquals(this, obj)) return true;

        if (GetType() != obj.GetType()) return false;
        Attributes<TS> other = (Attributes<TS>) obj;
        return AttributeValues.SequenceEqual(other.AttributeValues);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AttributeValues);
    }

    public override string ToString()
    {
        return Empty ? "no attributes present" : string.Join("; ", GetAttributeValues().Select(x => x.ToString()));
    }
    
    protected abstract AttributeNotFoundException CreateAttributeNotFoundException(string attributeName);
}