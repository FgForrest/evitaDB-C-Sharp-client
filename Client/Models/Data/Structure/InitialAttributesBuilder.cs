using System.Globalization;
using Client.Exceptions;
using Client.Models.Data.Mutations.Attributes;
using Client.Models.Schemas;
using Client.Utils;

namespace Client.Models.Data.Structure;

public class InitialAttributesBuilder : IAttributeBuilder
{
    private IEntitySchema EntitySchema { get; }
    private IReferenceSchema? ReferenceSchema { get; }
    private bool SuppressVerification { get; }
    private Dictionary<AttributeKey, AttributeValue> AttributeValues { get; }
    public bool AttributesAvailable => true;

    public InitialAttributesBuilder(IEntitySchema entitySchema)
    {
        EntitySchema = entitySchema;
        AttributeValues = new Dictionary<AttributeKey, AttributeValue>();
        SuppressVerification = false;
    }

    /**
	 * AttributesBuilder constructor that will be used for building brand new {@link Attributes} container.
	 */
    public InitialAttributesBuilder(IEntitySchema entitySchema, bool suppressVerification)
    {
        EntitySchema = entitySchema;
        AttributeValues = new Dictionary<AttributeKey, AttributeValue>();
        SuppressVerification = suppressVerification;
    }

    private static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string attributeName,
        Type? type
    )
    {
        IAttributeSchema? attributeSchema = entitySchema.GetAttribute(attributeName);
        VerifyAttributeIsInSchemaAndTypeMatch(entitySchema, null, attributeName, type, null, attributeSchema);
    }

    private static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string attributeName,
        Type type,
        CultureInfo locale
    )
    {
        IAttributeSchema? attributeSchema = entitySchema.GetAttribute(attributeName);
        VerifyAttributeIsInSchemaAndTypeMatch(entitySchema, null, attributeName, type, locale, attributeSchema);
    }

    private static void VerifyAttributeIsInSchemaAndTypeMatch(
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
    ) {
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

    public object GetAttribute(string attributeName)
    {
        throw new NotImplementedException();
    }

    public object GetAttribute(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public object[] GetAttributeArray(string attributeName)
    {
        throw new NotImplementedException();
    }

    public object[] GetAttributeArray(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public AttributeValue GetAttributeValue(string attributeName)
    {
        throw new NotImplementedException();
    }

    public AttributeValue GetAttributeValue(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public AttributeValue GetAttributeValue(AttributeKey attributeKey)
    {
        throw new NotImplementedException();
    }

    public IAttributeSchema GetAttributeSchema(string attributeName)
    {
        throw new NotImplementedException();
    }

    public ISet<string> GetAttributeNames()
    {
        throw new NotImplementedException();
    }

    public ISet<AttributeKey> GetAttributeKeys()
    {
        throw new NotImplementedException();
    }

    public ICollection<AttributeValue> GetAttributeValues()
    {
        throw new NotImplementedException();
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        throw new NotImplementedException();
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        throw new NotImplementedException();
    }

    public IAttributeBuilder RemoveAttribute(string attributeName)
    {
        AttributeKey attributeKey = new AttributeKey(attributeName);
        AttributeValues.Remove(attributeKey);
        return this;
    }

    public IAttributeBuilder SetAttribute(string attributeName, object? attributeValue)
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

    public IAttributeBuilder SetAttribute(string attributeName, object[]? attributeValue)
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

    public IAttributeBuilder RemoveAttribute(string attributeName, CultureInfo locale)
    {
        AttributeKey attributeKey = new AttributeKey(attributeName, locale);
        AttributeValues.Remove(attributeKey);
        return this;
    }

    public IAttributeBuilder SetAttribute(string attributeName, CultureInfo locale, object? attributeValue)
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

    public IAttributeBuilder SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
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

    public IAttributeBuilder MutateAttribute(AttributeMutation mutation)
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
        Dictionary<string, IAttributeSchema> newAttributes = AttributeValues
            .Where(entry => attributeSchemaProvider.GetAttribute(entry.Key.AttributeName) is not null)
            .Select(entry => entry.Value)
            .Select(IAttributeBuilder.CreateImplicitSchema)
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