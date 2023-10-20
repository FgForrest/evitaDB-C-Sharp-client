using System.Globalization;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Class supports intermediate mutable object that allows <see cref="Attributes"/> container rebuilding.
/// Due to performance reasons, there is special implementation or the situation when entity is newly created.
/// In this case we know everything is new and we don't need to closely monitor the changes so this can speed things up.
/// </summary>
public abstract class InitialAttributesBuilder<TS, T> : IAttributesBuilder<TS>
    where TS : class, IAttributeSchema
    where T : InitialAttributesBuilder<TS, T>
{
    protected IEntitySchema EntitySchema { get; }
    private bool SuppressVerification { get; }
    protected Dictionary<AttributeKey, AttributeValue> AttributeValues { get; }
    public bool AttributesAvailable() => true;
    public bool AttributesAvailable(CultureInfo locale) => true;
    public bool AttributeAvailable(string attributeName) => true;
    public bool AttributeAvailable(string attributeName, CultureInfo locale) => true;

    public InitialAttributesBuilder(IEntitySchema entitySchema)
    {
        EntitySchema = entitySchema;
        AttributeValues = new Dictionary<AttributeKey, AttributeValue>();
        SuppressVerification = false;
    }

    public InitialAttributesBuilder(IEntitySchema entitySchema, bool suppressVerification)
    {
        EntitySchema = entitySchema;
        AttributeValues = new Dictionary<AttributeKey, AttributeValue>();
        SuppressVerification = suppressVerification;
    }
    
    public object? GetAttribute(string attributeName)
    {
        if (AttributeValues.TryGetValue(new AttributeKey(attributeName), out AttributeValue? attributeValue))
        {
            return attributeValue.Value;
        }
        return null;
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        if (AttributeValues.TryGetValue(new AttributeKey(attributeName, locale), out AttributeValue? attributeValue))
        {
            return attributeValue.Value;
        }
        return null;
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        if (AttributeValues.TryGetValue(new AttributeKey(attributeName), out AttributeValue? attributeValue))
        {
            return attributeValue.Value as object[];
        }
        return null;
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        if (AttributeValues.TryGetValue(new AttributeKey(attributeName, locale), out AttributeValue? attributeValue))
        {
            return attributeValue.Value as object[];
        }
        return null;
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        return AttributeValues.TryGetValue(new AttributeKey(attributeName), out AttributeValue? attributeValue)
            ? attributeValue
            : null;
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        return AttributeValues.TryGetValue(new AttributeKey(attributeName, locale), out AttributeValue? attributeValue)
            ? attributeValue
            : null;
    }

    public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
    {
        if (AttributeValues.TryGetValue(attributeKey, out AttributeValue? value))
        {
            return value;
        }

        if (attributeKey.Localized && AttributeValues.TryGetValue(
                new AttributeKey(attributeKey.AttributeName),
                out AttributeValue? globalAttributeValue))
        {
            return globalAttributeValue;
        }

        return null;
    }

    public TS? GetAttributeSchema(string attributeName)
    {
        return EntitySchema.GetAttribute(attributeName) as TS;
    }

    public ISet<string> GetAttributeNames()
    {
        return AttributeValues.Keys.Select(x=>x.AttributeName).ToHashSet();
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
        return GetAttributeValues()
            .Where(it => attributeName.Equals(it.Key.AttributeName))
            .ToList();
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        return AttributeValues
            .Keys
            .Select(x => x.Locale)
            .Where(x => x is not null)
            .ToHashSet()!;
    }

    public IAttributesBuilder<TS> RemoveAttribute(string attributeName)
    {
        AttributeKey attributeKey = new AttributeKey(attributeName);
        AttributeValues.Remove(attributeKey);
        return this;
    }

    public IAttributesBuilder<TS> SetAttribute(string attributeName, object? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName);
        }

        AttributeKey attributeKey = new AttributeKey(attributeName);
        if (!SuppressVerification)
        {
            AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, attributeName, attributeValue.GetType(), GetLocationResolver());
        }

        AttributeValues.Add(attributeKey, new AttributeValue(1, attributeKey, attributeValue));
        return this;
    }

    public IAttributesBuilder<TS> SetAttribute(string attributeName, object[]? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName);
        }

        AttributeKey attributeKey = new AttributeKey(attributeName);
        if (!SuppressVerification)
        {
            AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, attributeName, attributeValue.GetType(), GetLocationResolver());
        }

        AttributeValues.Add(attributeKey, new AttributeValue(1, attributeKey, attributeValue));
        return this;
    }

    public IAttributesBuilder<TS> RemoveAttribute(string attributeName, CultureInfo locale)
    {
        AttributeKey attributeKey = new AttributeKey(attributeName, locale);
        AttributeValues.Remove(attributeKey);
        return this;
    }

    public IAttributesBuilder<TS> SetAttribute(string attributeName, CultureInfo locale, object? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName, locale);
        }

        AttributeKey attributeKey = new AttributeKey(attributeName, locale);
        if (!SuppressVerification)
        {
            AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, attributeName, attributeValue.GetType(), locale, GetLocationResolver());
        }

        AttributeValues.Add(attributeKey, new AttributeValue(1, attributeKey, attributeValue));
        return this;
    }

    public IAttributesBuilder<TS> SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName, locale);
        }

        AttributeKey attributeKey = new AttributeKey(attributeName, locale);
        if (!SuppressVerification)
        {
            AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, attributeName, attributeValue.GetType(), locale, GetLocationResolver());
        }

        AttributeValues.Add(attributeKey, new AttributeValue(1, attributeKey, attributeValue));
        return this;
    }

    public IAttributesBuilder<TS> MutateAttribute(AttributeMutation mutation)
    {
        throw new NotSupportedException("You cannot apply mutation when entity is just being created!");
    }

    public IEnumerable<AttributeMutation> BuildChangeSet()
    {
        throw new NotSupportedException("Initial entity creation doesn't support change monitoring - it has no sense.");
    }

    public abstract Attributes<TS> Build();

    public abstract Func<string> GetLocationResolver();
}