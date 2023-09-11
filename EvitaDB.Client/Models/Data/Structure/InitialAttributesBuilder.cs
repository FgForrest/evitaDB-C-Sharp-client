using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Class supports intermediate mutable object that allows <see cref="Attributes"/> container rebuilding.
/// Due to performance reasons, there is special implementation or the situation when entity is newly created.
/// In this case we know everything is new and we don't need to closely monitor the changes so this can speed things up.
/// </summary>
public class InitialAttributesBuilder : IAttributesBuilder
{
    private IEntitySchema EntitySchema { get; }
    private IReferenceSchema? ReferenceSchema { get; }
    private bool SuppressVerification { get; }
    private Dictionary<AttributeKey, AttributeValue> AttributeValues { get; }
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

    internal static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string attributeName,
        Type? type
    )
    {
        IAttributeSchema? attributeSchema = entitySchema.GetAttribute(attributeName);
        VerifyAttributeIsInSchemaAndTypeMatch(entitySchema, null, attributeName, type, null, attributeSchema);
    }

    internal static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string attributeName,
        Type type,
        CultureInfo locale
    )
    {
        IAttributeSchema? attributeSchema = entitySchema.GetAttribute(attributeName);
        VerifyAttributeIsInSchemaAndTypeMatch(entitySchema, null, attributeName, type, locale, attributeSchema);
    }

    internal static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema,
        string attributeName,
        Type? type,
        CultureInfo? locale,
        IAttributeSchema? attributeSchema
    )
    {
        Assert.IsTrue(
            attributeSchema != null || entitySchema.Allows(EvolutionMode.AddingAttributes),
            () => new InvalidMutationException(
                "Attribute " + attributeName + " is not configured in entity `" + entitySchema.Name + "`" +
                (referenceSchema == null ? "" : " reference `" + referenceSchema.Name + "`") +
                " schema and automatic evolution is not enabled for attributes!"
            )
        );
        if (attributeSchema != null)
        {
            if (type != null)
            {
                Assert.IsTrue(attributeSchema.Type.IsAssignableFrom(type),
                    () => new InvalidDataTypeMutationException(
                        "Attribute " + attributeName + " in entity `" + entitySchema.Name + "`" +
                        (referenceSchema == null ? "" : " reference `" + referenceSchema.Name + "`") +
                        " schema accepts only type " + attributeSchema.Type.Name +
                        " - value type is different: " + type.Name + "!",
                        attributeSchema.Type, type
                    )
                );
                if (attributeSchema.Sortable)
                {
                    Assert.IsTrue(!type.IsArray,
                        () => new InvalidDataTypeMutationException(
                            "Attribute " + attributeName + " in entity `" + entitySchema.Name + "`" +
                            (referenceSchema == null ? "" : " reference `" + referenceSchema.Name + "`") +
                            " schema is sortable and can't hold arrays of " + type.Name + "!",
                            attributeSchema.Type, type
                        )
                    );
                }
            }

            if (locale == null)
            {
                Assert.IsTrue(
                    !attributeSchema.Localized,
                    () => new InvalidMutationException(
                        "Attribute `" + attributeName + "` in entity `" + entitySchema.Name + "`" +
                        (referenceSchema == null ? "" : " reference `" + referenceSchema.Name + "`") +
                        " schema is localized and doesn't accept non-localized attributes!"
                    )
                );
            }
            else
            {
                Assert.IsTrue(
                    attributeSchema.Localized,
                    () => new InvalidMutationException(
                        "Attribute `" + attributeName + "` in entity `" + entitySchema.Name + "`" +
                        (referenceSchema == null ? "" : " reference `" + referenceSchema.Name + "`") +
                        " schema is not localized and doesn't accept localized attributes!"
                    )
                );
                Assert.IsTrue(
                    entitySchema.SupportsLocale(locale) || entitySchema.Allows(EvolutionMode.AddingLocales),
                    () => new InvalidMutationException(
                        "Attribute `" + attributeName + "` in entity `" + entitySchema.Name + "`" +
                        (referenceSchema == null ? "" : " reference `" + referenceSchema.Name + "`") +
                        " schema is localized, but schema doesn't support locale " + locale + "! " +
                        "Supported locales are: " +
                        string.Join(", ", entitySchema.Locales.Select(x => x.IetfLanguageTag))
                    )
                );
            }
        }
        else if (locale != null)
        {
            // at least verify supported locale
            Assert.IsTrue(
                entitySchema.SupportsLocale(locale) || entitySchema.Allows(EvolutionMode.AddingLocales),
                () => new InvalidMutationException(
                    "Attribute `" + attributeName + "` in entity `" + entitySchema.Name + "`" +
                    (referenceSchema == null ? "" : " reference `" + referenceSchema.Name + "`") +
                    " schema is localized, but schema doesn't support locale " + locale + "! " +
                    "Supported locales are: " +
                    string.Join(", ", entitySchema.Locales.Select(x => x.IetfLanguageTag))
                )
            );
        }
    }

    internal InitialAttributesBuilder(
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema
    )
    {
        EntitySchema = entitySchema;
        ReferenceSchema = referenceSchema;
        AttributeValues = new Dictionary<AttributeKey, AttributeValue>();
        SuppressVerification = false;
    }

    internal InitialAttributesBuilder(
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema,
        bool suppressVerification
    )
    {
        EntitySchema = entitySchema;
        ReferenceSchema = referenceSchema;
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

    public IAttributeSchema? GetAttributeSchema(string attributeName)
    {
        return EntitySchema.GetAttribute(attributeName);
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

    public IAttributesBuilder RemoveAttribute(string attributeName)
    {
        AttributeKey attributeKey = new AttributeKey(attributeName);
        AttributeValues.Remove(attributeKey);
        return this;
    }

    public IAttributesBuilder SetAttribute(string attributeName, object? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName);
        }

        AttributeKey attributeKey = new AttributeKey(attributeName);
        if (!SuppressVerification)
        {
            VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, attributeName, attributeValue.GetType());
        }

        AttributeValues.Add(attributeKey, new AttributeValue(1, attributeKey, attributeValue));
        return this;
    }

    public IAttributesBuilder SetAttribute(string attributeName, object[]? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName);
        }

        AttributeKey attributeKey = new AttributeKey(attributeName);
        if (!SuppressVerification)
        {
            VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, attributeName, attributeValue.GetType());
        }

        AttributeValues.Add(attributeKey, new AttributeValue(1, attributeKey, attributeValue));
        return this;
    }

    public IAttributesBuilder RemoveAttribute(string attributeName, CultureInfo locale)
    {
        AttributeKey attributeKey = new AttributeKey(attributeName, locale);
        AttributeValues.Remove(attributeKey);
        return this;
    }

    public IAttributesBuilder SetAttribute(string attributeName, CultureInfo locale, object? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName, locale);
        }

        AttributeKey attributeKey = new AttributeKey(attributeName, locale);
        if (!SuppressVerification)
        {
            VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, attributeName, attributeValue.GetType(), locale);
        }

        AttributeValues.Add(attributeKey, new AttributeValue(1, attributeKey, attributeValue));
        return this;
    }

    public IAttributesBuilder SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName, locale);
        }

        AttributeKey attributeKey = new AttributeKey(attributeName, locale);
        if (!SuppressVerification)
        {
            VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, attributeName, attributeValue.GetType(), locale);
        }

        AttributeValues.Add(attributeKey, new AttributeValue(1, attributeKey, attributeValue));
        return this;
    }

    public IAttributesBuilder MutateAttribute(AttributeMutation mutation)
    {
        throw new NotSupportedException("You cannot apply mutation when entity is just being created!");
    }

    public IEnumerable<AttributeMutation> BuildChangeSet()
    {
        throw new NotSupportedException("Initial entity creation doesn't support change monitoring - it has no sense.");
    }

    public Attributes Build()
    {
        IAttributeSchemaProvider<IAttributeSchema> attributeSchemaProvider =
            ReferenceSchema as IAttributeSchemaProvider<IAttributeSchema> ?? EntitySchema;
        IDictionary<string, IAttributeSchema> newAttributes = AttributeValues
            .Where(entry => attributeSchemaProvider.GetAttribute(entry.Key.AttributeName) is not null)
            .Select(entry => entry.Value)
            .Select(IAttributesBuilder.CreateImplicitSchema)
            .ToDictionary(
                attributeType => attributeType.Name,
                attributeType => attributeType
            );
        return new Attributes(
            EntitySchema,
            ReferenceSchema,
            AttributeValues.Values,
            !newAttributes.Any()
                ? attributeSchemaProvider.GetAttributes()
                : attributeSchemaProvider.GetAttributes()
                    .Concat(newAttributes)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value
                    )
        );
    }
}