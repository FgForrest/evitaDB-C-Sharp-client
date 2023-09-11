using System.Collections.Immutable;
using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;
using Newtonsoft.Json;

namespace EvitaDB.Client.Models.Data.Structure;

public class Attributes : IAttributes
{
    [JsonIgnore] internal IEntitySchema EntitySchema { get; }
    [JsonIgnore] internal IReferenceSchema? ReferenceSchema { get; }
    internal Dictionary<AttributeKey, AttributeValue> AttributeValues { get; }
    [JsonIgnore] public IDictionary<string, IAttributeSchema> AttributeTypes { get; }
    private ISet<string>? AttributeNames { get; set; }
    private ISet<CultureInfo>? AttributeLocales { get; set; }
    public bool Empty => AttributeValues.Count == 0;
    public bool AttributesAvailable() => true;
    public bool AttributesAvailable(CultureInfo locale) => true;
    public bool AttributeAvailable(string attributeName) => true;
    public bool AttributeAvailable(string attributeName, CultureInfo locale) => true;

    public Attributes(IEntitySchema entitySchema,
        IEnumerable<AttributeValue> attributeValues,
        IDictionary<string, IAttributeSchema> attributeTypes)
    {
        EntitySchema = entitySchema;
        AttributeValues = attributeValues.ToDictionary(x => x.Key, x => x);
        AttributeTypes = attributeTypes;
    }

    public Attributes(IEntitySchema entitySchema, ICollection<AttributeValue> attributeValues)
    {
        EntitySchema = entitySchema;
        AttributeValues = attributeValues.GroupBy(x => x.Key).Any(g => g.Count() > 1)
            ? throw new EvitaInvalidUsageException("Duplicated attribute keys are not allowed!")
            : attributeValues.ToDictionary(x => x.Key, x => x);
        AttributeTypes = attributeValues
            .Select(x => x.Key.AttributeName).Distinct()
            .Select(entitySchema.GetAttribute)
            .Where(x => x is not null)
            .ToImmutableDictionary(x => x!.Name, x => x!);
    }

    public Attributes(IEntitySchema entitySchema)
    {
        EntitySchema = entitySchema;
        AttributeValues = new Dictionary<AttributeKey, AttributeValue>();
        AttributeTypes = new Dictionary<string, IAttributeSchema>(EntitySchema.Attributes).ToImmutableDictionary();
        AttributeLocales = new HashSet<CultureInfo>();
    }

    public Attributes(
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema,
        ICollection<AttributeValue> attributeValues,
        IDictionary<string, IAttributeSchema> attributeTypes
    )
    {
        EntitySchema = entitySchema;
        ReferenceSchema = referenceSchema;
        AttributeValues = attributeValues.ToDictionary(x => x.Key, x => x);
        AttributeTypes = attributeTypes;
    }

    public object? GetAttribute(string attributeName)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out IAttributeSchema? attributeSchema))
        {
            if (ReferenceSchema is null)
            {
                throw new AttributeNotFoundException(attributeName, EntitySchema);
            }

            throw new AttributeNotFoundException(attributeName, ReferenceSchema, EntitySchema);
        }

        Assert.IsTrue(!attributeSchema.Localized,
            () => ContextMissingException.LocaleForAttributeContextMissing(attributeName));
        return AttributeValues.TryGetValue(new AttributeKey(attributeName), out var attributeValue)
            ? attributeValue.Value
            : null;
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out IAttributeSchema? attributeSchema))
        {
            if (ReferenceSchema is null)
            {
                throw new AttributeNotFoundException(attributeName, EntitySchema);
            }

            throw new AttributeNotFoundException(attributeName, ReferenceSchema, EntitySchema);
        }

        Assert.IsTrue(!attributeSchema.Localized,
            () => ContextMissingException.LocaleForAttributeContextMissing(attributeName));
        return AttributeValues.TryGetValue(new AttributeKey(attributeName), out var attributeValue)
            ? (object[]?) attributeValue.Value
            : null;
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out IAttributeSchema? attributeSchema))
        {
            if (ReferenceSchema is null)
            {
                throw new AttributeNotFoundException(attributeName, EntitySchema);
            }

            throw new AttributeNotFoundException(attributeName, ReferenceSchema, EntitySchema);
        }

        return attributeSchema.Localized ? null :
            AttributeValues.TryGetValue(new AttributeKey(attributeName), out var attributeValue) ? attributeValue :
            null;
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out IAttributeSchema? attributeSchema))
        {
            if (ReferenceSchema is null)
            {
                throw new AttributeNotFoundException(attributeName, EntitySchema);
            }

            throw new AttributeNotFoundException(attributeName, ReferenceSchema, EntitySchema);
        }

        Assert.IsTrue(!attributeSchema.Localized,
            () => ContextMissingException.LocaleForAttributeContextMissing(attributeName));
        return AttributeValues.TryGetValue(new AttributeKey(attributeName), out var attributeValue)
            ? attributeValue.Value
            : null;
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out IAttributeSchema? attributeSchema))
        {
            if (ReferenceSchema is null)
            {
                throw new AttributeNotFoundException(attributeName, EntitySchema);
            }

            throw new AttributeNotFoundException(attributeName, ReferenceSchema, EntitySchema);
        }

        AttributeKey attributeKey = attributeSchema.Localized
            ? new AttributeKey(attributeName, locale)
            : new AttributeKey(attributeName);
        return AttributeValues.TryGetValue(attributeKey, out var attributeValue)
            ? (object[]?) attributeValue.Value
            : null;
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        if (!AttributeTypes.TryGetValue(attributeName, out IAttributeSchema? attributeSchema))
        {
            if (ReferenceSchema is null)
            {
                throw new AttributeNotFoundException(attributeName, EntitySchema);
            }

            throw new AttributeNotFoundException(attributeName, ReferenceSchema, EntitySchema);
        }

        AttributeKey attributeKey = attributeSchema.Localized
            ? new AttributeKey(attributeName, locale)
            : new AttributeKey(attributeName);
        return AttributeValues.TryGetValue(attributeKey, out var attributeValue) ? attributeValue : null;
    }

    public IAttributeSchema? GetAttributeSchema(string attributeName)
    {
        return AttributeTypes.TryGetValue(attributeName, out var attributeSchema) ? attributeSchema : null;
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
        if (!AttributeTypes.TryGetValue(attributeName, out IAttributeSchema? attributeSchema))
        {
            if (ReferenceSchema is null)
            {
                throw new AttributeNotFoundException(attributeName, EntitySchema);
            }

            throw new AttributeNotFoundException(attributeName, ReferenceSchema, EntitySchema);
        }

        AttributeKey attributeKeyToUse = attributeSchema.Localized
            ? attributeKey
            : attributeKey.Localized ? new AttributeKey(attributeName) : attributeKey;
        return AttributeValues.TryGetValue(attributeKeyToUse, out var attributeValue) ? attributeValue : null;
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

    public override string ToString()
    {
        return Empty ? "no attributes present" : string.Join("; ", GetAttributeValues().Select(x => x.ToString()));
    }
}