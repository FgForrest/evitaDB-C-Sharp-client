using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Class supports intermediate mutable object that allows <see cref="Attributes"/> container rebuilding.
/// We need to closely monitor what attribute is changed and how. These changes are wrapped in so called mutations
/// (see <see cref="AttributeMutation"/> and its implementations) and mutations can be then processed transactionally by
/// the engine.
/// </summary>
public abstract class ExistingAttributesBuilder<TS, T> : IAttributesBuilder<TS>
    where TS : IAttributeSchema
    where T : ExistingAttributesBuilder<TS, T>
{
    private IEntitySchema EntitySchema { get; }
    protected Attributes<TS> BaseAttributes { get; }
    private bool SuppressVerification { get; }
    private IDictionary<AttributeKey, AttributeMutation> AttributeMutations { get; }

    public ExistingAttributesBuilder(IEntitySchema entitySchema,
        ICollection<AttributeValue> attributes, IDictionary<string, TS> attributeTypes)
    {
        EntitySchema = entitySchema;
        AttributeMutations = new Dictionary<AttributeKey, AttributeMutation>();
        BaseAttributes = CreateAttributesContainer(entitySchema, attributes, attributeTypes);
        SuppressVerification = false;
    }

    internal ExistingAttributesBuilder(IEntitySchema entitySchema,
        ICollection<AttributeValue> attributes, IDictionary<string, TS> attributeTypes,
        bool suppressVerification)
    {
        EntitySchema = entitySchema;
        AttributeMutations = new Dictionary<AttributeKey, AttributeMutation>();
        BaseAttributes = CreateAttributesContainer(entitySchema, attributes, attributeTypes);
        SuppressVerification = suppressVerification;
    }

    public ExistingAttributesBuilder(IEntitySchema entitySchema, Attributes<TS> attributes)
    {
        EntitySchema = entitySchema;
        AttributeMutations = new Dictionary<AttributeKey, AttributeMutation>();
        BaseAttributes = attributes;
        SuppressVerification = false;
    }

    internal ExistingAttributesBuilder(IEntitySchema entitySchema, Attributes<TS> attributes, bool suppressVerification)
    {
        EntitySchema = entitySchema;
        AttributeMutations = new Dictionary<AttributeKey, AttributeMutation>();
        BaseAttributes = attributes;
        SuppressVerification = suppressVerification;
    }

    public bool AttributesAvailable()
    {
        return BaseAttributes.AttributesAvailable();
    }

    public bool AttributesAvailable(CultureInfo locale)
    {
        return BaseAttributes.AttributesAvailable(locale);
    }

    public bool AttributeAvailable(string attributeName) => BaseAttributes.AttributeAvailable(attributeName);

    public bool AttributeAvailable(string attributeName, CultureInfo locale) =>
        BaseAttributes.AttributeAvailable(attributeName, locale);

    public T AddMutation(AttributeMutation localMutation)
    {
        if (localMutation is UpsertAttributeMutation upsertAttributeMutation)
        {
            AttributeKey attributeKey = upsertAttributeMutation.AttributeKey;
            object attributeValue = upsertAttributeMutation.Value;
            if (!SuppressVerification)
            {
                AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(
                    BaseAttributes.EntitySchema, attributeKey.AttributeName,
                    attributeValue.GetType(), attributeKey.Locale!, GetLocationResolver()
                );
            }

            AttributeMutations.Add(attributeKey, upsertAttributeMutation);
        }
        else if (localMutation is RemoveAttributeMutation removeAttributeMutation)
        {
            AttributeKey attributeKey = removeAttributeMutation.AttributeKey;
            VerifyAttributeExists(attributeKey);
            if (BaseAttributes.GetAttributeValueWithoutSchemaCheck(attributeKey) is null)
            {
                AttributeMutations.Remove(attributeKey);
            }
            else
            {
                AttributeMutations.Add(attributeKey, removeAttributeMutation);
            }
        }
        else if (localMutation is ApplyDeltaAttributeMutation applyDeltaAttributeMutation)
        {
            AttributeKey attributeKey = applyDeltaAttributeMutation.AttributeKey;
            AttributeValue? attributeValue = BaseAttributes.GetAttributeValueWithoutSchemaCheck(attributeKey);
            if (AttributeMutations.TryGetValue(attributeKey, out AttributeMutation? mutation))
            {
                attributeValue = mutation is not null
                    ? mutation.MutateLocal(EntitySchema, attributeValue)
                    : throw new EvitaInvalidUsageException("Attribute with name `" + attributeKey + "` doesn't exist!");
            }

            AttributeValue updatedValue = applyDeltaAttributeMutation.MutateLocal(EntitySchema, attributeValue);
            if (!AttributeMutations.TryGetValue(attributeKey, out _))
            {
                AttributeMutations.Add(attributeKey, applyDeltaAttributeMutation);
            }
            else
            {
                AttributeMutations.Add(attributeKey, new UpsertAttributeMutation(attributeKey, updatedValue.Value!));
            }
        }
        else
        {
            throw new EvitaInternalError("Unknown Evita attribute mutation: `" + localMutation.GetType() + "`!");
        }

        return (this as T)!;
    }

    public object? GetAttribute(string attributeName)
    {
        return GetAttributeValueInternal(new AttributeKey(attributeName))?.Value;
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        return GetAttributeValueInternal(new AttributeKey(attributeName, locale))?.Value;
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        return GetAttributeValueInternal(new AttributeKey(attributeName))?.Value as object[];
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        return GetAttributeValueInternal(new AttributeKey(attributeName, locale))?.Value as object[];
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        return GetAttributeValueInternal(new AttributeKey(attributeName));
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        return GetAttributeValueInternal(new AttributeKey(attributeName, locale));
    }

    public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
    {
        return GetAttributeValueInternal(attributeKey) ??
               (attributeKey.Localized
                   ? GetAttributeValueInternal(new AttributeKey(attributeKey.AttributeName))
                   : null);
    }

    public TS? GetAttributeSchema(string attributeName)
    {
        return BaseAttributes.GetAttributeSchema(attributeName);
    }

    public ISet<string> GetAttributeNames()
    {
        return GetAttributeValues().Select(it => it.Key.AttributeName).ToHashSet();
    }

    public ISet<AttributeKey> GetAttributeKeys()
    {
        return GetAttributeValues()
            .Select(x => x.Key)
            .ToHashSet();
    }

    public ICollection<AttributeValue> GetAttributeValues()
    {
        return GetAttributeValuesWithoutPredicate().ToList();
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        return GetAttributeValuesWithoutPredicate()
            .Where(x => x.Key.AttributeName == attributeName).ToList();
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        return GetAttributeValues()
            .Select(it => it.Key.Locale)
            .Where(x => x is not null)
            .ToHashSet()!;
    }

    public IAttributesBuilder<TS> RemoveAttribute(string attributeName)
    {
        AttributeKey attributeKey = new AttributeKey(attributeName);
        VerifyAttributeExists(attributeKey);
        AttributeMutations.Add(
            attributeKey,
            new RemoveAttributeMutation(attributeKey)
        );
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
            AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(BaseAttributes.EntitySchema, attributeName,
                attributeValue.GetType(), GetLocationResolver());
        }

        AttributeMutations.Add(
            attributeKey,
            new UpsertAttributeMutation(attributeKey, attributeValue)
        );
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
            AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(BaseAttributes.EntitySchema, attributeName,
                attributeValue.GetType(), GetLocationResolver());
        }

        AttributeMutations.Add(
            attributeKey,
            new UpsertAttributeMutation(attributeKey, attributeValue)
        );
        return this;
    }

    public IAttributesBuilder<TS> RemoveAttribute(string attributeName, CultureInfo locale)
    {
        AttributeKey attributeKey = new AttributeKey(attributeName, locale);
        VerifyAttributeExists(attributeKey);
        AttributeMutations.Add(
            attributeKey,
            new RemoveAttributeMutation(attributeKey)
        );
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
            AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(BaseAttributes.EntitySchema,
               attributeName, attributeValue.GetType(), locale, GetLocationResolver());
        }

        AttributeMutations.Add(
            attributeKey,
            new UpsertAttributeMutation(attributeKey, attributeValue)
        );
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
            AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(BaseAttributes.EntitySchema,
                attributeName, attributeValue.GetType(), locale, GetLocationResolver());
        }

        AttributeMutations.Add(
            attributeKey,
            new UpsertAttributeMutation(attributeKey, attributeValue)
        );
        return this;
    }

    public IAttributesBuilder<TS> MutateAttribute(AttributeMutation mutation)
    {
        AttributeMutations.Add(mutation.AttributeKey, mutation);
        return this;
    }

    public IEnumerable<AttributeMutation> BuildChangeSet()
    {
        IDictionary<AttributeKey, AttributeValue> builtAttributes =
            new Dictionary<AttributeKey, AttributeValue>(BaseAttributes.AttributeValues);
        return AttributeMutations.Values
            .Where(it =>
            {
                AttributeValue? existingValue = builtAttributes.TryGetValue(it.AttributeKey, out AttributeValue? value)
                    ? value
                    : null;
                AttributeValue newAttribute = it.MutateLocal(EntitySchema, existingValue);
                builtAttributes.Add(it.AttributeKey, newAttribute);
                return existingValue == null || newAttribute.Version > existingValue.Version;
            });
    }

    public abstract Attributes<TS> Build();

    protected List<AttributeValue> GetAttributeValuesWithoutPredicate()
    {
        List<AttributeValue> result = new List<AttributeValue>();
        foreach (var (key, value) in BaseAttributes.AttributeValues)
        {
            if (AttributeMutations.TryGetValue(key, out AttributeMutation? attributeMutation))
            {
                AttributeValue mutatedAttribute = attributeMutation.MutateLocal(EntitySchema, value);
                result.Add(mutatedAttribute.DiffersFrom(value) ? mutatedAttribute : value);
            }
            else
            {
                result.Add(value);
            }
        }

        result.AddRange(
            AttributeMutations.Values
                .Where(it => !BaseAttributes.AttributeValues.ContainsKey(it.AttributeKey))
                .Select(it => it.MutateLocal(EntitySchema, null))
        );

        return result;
    }

    internal bool AnyChangeInMutations()
    {
        return BaseAttributes.AttributeValues
            .Select(it =>
                AttributeMutations.TryGetValue(it.Key, out AttributeMutation? attributeMutation) &&
                attributeMutation.MutateLocal(EntitySchema, it.Value).DiffersFrom(it.Value))
            .Concat(AttributeMutations
                .Values
                .Where(it => !BaseAttributes.AttributeValues.ContainsKey(it.AttributeKey))
                .Select(_ => true))
            .Any(t => t);
    }

    public bool Differs(Attributes<TS> attributes) => !BaseAttributes.Equals(attributes);

    private void VerifyAttributeExists(AttributeKey attributeKey)
    {
        Assert.IsTrue(
            BaseAttributes.GetAttributeValueWithoutSchemaCheck(attributeKey) is not null ||
            AttributeMutations.TryGetValue(attributeKey, out AttributeMutation? mutation) &&
            mutation is UpsertAttributeMutation,
            "Attribute `" + attributeKey + "` doesn't exist!"
        );
    }

    private AttributeValue? GetAttributeValueInternal(AttributeKey attributeKey)
    {
        AttributeValue? attributeValue =
            BaseAttributes.AttributeValues.TryGetValue(attributeKey, out AttributeValue? value) ? value : null;
        return AttributeMutations.TryGetValue(attributeKey, out AttributeMutation? mutation)
            ? mutation.MutateLocal(EntitySchema, attributeValue)
            : null;
    }

    /// <summary>
    /// Creates new container for attributes.
    /// </summary>
    protected abstract Attributes<TS> CreateAttributesContainer(
        IEntitySchema entitySchema,
        ICollection<AttributeValue> attributes,
        IDictionary<string, TS> attributeTypes
    );

    public abstract Func<string> GetLocationResolver();
}