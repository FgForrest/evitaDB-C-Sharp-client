using System.Collections.Immutable;
using System.Globalization;
using Client.Exceptions;
using Client.Models.Schemas;
using Newtonsoft.Json;

namespace Client.Models.Data.Structure;

public class Attributes : IAttributes
{
    [JsonIgnore]
    private IEntitySchema EntitySchema { get; }
    [JsonIgnore]
    private IReferenceSchema? ReferenceSchema { get; }
    private Dictionary<AttributeKey, AttributeValue> AttributeValues { get; }
    [JsonIgnore]
    public IDictionary<string, IAttributeSchema> AttributeTypes { get; }
    private ISet<string>? AttributeNames { get; set; }
    private ISet<CultureInfo>? AttributeLocales { get; set; }
    public bool Empty => AttributeValues.Count == 0;
    public bool AttributesAvailable => true;

    public Attributes(IEntitySchema entitySchema, 
        IEnumerable<AttributeValue> attributeValues,
        Dictionary<string, IAttributeSchema> attributeTypes)
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
        return AttributeValues.Values.FirstOrDefault(x => x.Key.AttributeName == attributeName)?.Value;
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        return AttributeValues.Values.FirstOrDefault(x => x.Key.AttributeName == attributeName)?.Value as object[];
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        return AttributeValues.Values.FirstOrDefault(x => x.Key.AttributeName == attributeName);
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        var attributeValue = AttributeValues.Values.FirstOrDefault(x =>
            x.Key.AttributeName == attributeName && x.Key.Locale?.IetfLanguageTag == locale.IetfLanguageTag);
        return attributeValue is null ? GetAttribute(attributeName) : attributeValue.Value;
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        var attributeValue = AttributeValues.Values.FirstOrDefault(x =>
            x.Key.AttributeName == attributeName && x.Key.Locale?.IetfLanguageTag == locale.IetfLanguageTag);
        return attributeValue is null ? GetAttribute(attributeName) as object[] : attributeValue.Value as object[];
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        var attributeValue = AttributeValues.Values.FirstOrDefault(x =>
            x.Key.AttributeName == attributeName && x.Key.Locale?.IetfLanguageTag == locale.IetfLanguageTag);
        var attributeKey = AttributeValues.Values.FirstOrDefault(x =>
            x.Key.AttributeName == attributeName)?.Key;
        if (attributeKey != null)
            return attributeValue ?? AttributeValues[attributeKey];
        return null;
    }

    public IAttributeSchema GetAttributeSchema(string attributeName)
    {
        return AttributeTypes[attributeName];
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
        return AttributeValues.Values;
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        return AttributeValues
            .Where(it => attributeName == it.Key.AttributeName)
            .Select(x => x.Value)
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
        return AttributeValues.TryGetValue(attributeKey, out var attributeValue) ? attributeValue : null;
    }

    public override string ToString()
    {
        return Empty ? "no attributes present" : string.Join("; ", GetAttributeValues().Select(x=>x.ToString()));
    }
}