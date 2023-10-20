using System.Globalization;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Mutations.Reference;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Structure;

public class InitialReferenceBuilder : IReferenceBuilder
{
    private IEntitySchema EntitySchema { get; }
    private IAttributesBuilder<IAttributeSchema> AttributesBuilder { get; }
    public ReferenceKey ReferenceKey { get; }

    public GroupEntityReference? Group =>
        GroupId is null ? null : new GroupEntityReference(GroupType!, GroupId.Value, 1);

    public ISealedEntity? GroupEntity => null;
    public ISealedEntity? ReferencedEntity => null;
    public IReferenceSchema? ReferenceSchema => EntitySchema.GetReference(ReferenceName);
    public Cardinality? ReferenceCardinality { get; }
    public string? ReferencedEntityType { get; }
    public string ReferenceName => ReferenceKey.ReferenceName;
    public int ReferencedPrimaryKey => ReferenceKey.PrimaryKey;
    private string? GroupType { get; set; }
    private int? GroupId { get; set; }
    public int Version => 1;
    public bool Dropped => false;
    
    public InitialReferenceBuilder(
        IEntitySchema entitySchema,
        string referenceName,
        int referencedEntityPrimaryKey,
        Cardinality? referenceCardinality,
        string? referencedEntityType
    )
    {
        EntitySchema = entitySchema;
        ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
        ReferenceCardinality = referenceCardinality;
        ReferencedEntityType = referencedEntityType;
        GroupId = null;
        GroupType = null;
        AttributesBuilder = new InitialReferenceAttributesBuilder(
            entitySchema,
            entitySchema
                .GetReference(referenceName) ?? Reference
                .CreateImplicitSchema(referenceName, referencedEntityType, referenceCardinality, null),
            true
        );
    }


    public bool AttributesAvailable()
    {
        return AttributesBuilder.AttributesAvailable();
    }

    public bool AttributesAvailable(CultureInfo locale)
    {
        return AttributesBuilder.AttributesAvailable(locale);
    }

    public bool AttributeAvailable(string attributeName)
    {
        return AttributesBuilder.AttributeAvailable(attributeName);
    }

    public bool AttributeAvailable(string attributeName, CultureInfo locale)
    {
        return AttributesBuilder.AttributeAvailable(attributeName, locale);
    }

    public object? GetAttribute(string attributeName)
    {
        return AttributesBuilder.GetAttribute(attributeName);
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        return AttributesBuilder.GetAttribute(attributeName, locale);
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        return AttributesBuilder.GetAttributeArray(attributeName);
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        return AttributesBuilder.GetAttributeArray(attributeName, locale);
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        return AttributesBuilder.GetAttributeValue(attributeName);
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        return AttributesBuilder.GetAttributeValue(attributeName, locale);
    }

    public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
    {
        return AttributesBuilder.GetAttributeValue(attributeKey);
    }

    public IAttributeSchema? GetAttributeSchema(string attributeName)
    {
        return AttributesBuilder.GetAttributeSchema(attributeName);
    }

    public ISet<string> GetAttributeNames()
    {
        return AttributesBuilder.GetAttributeNames();
    }

    public ISet<AttributeKey> GetAttributeKeys()
    {
        return AttributesBuilder.GetAttributeKeys();
    }

    public ICollection<AttributeValue> GetAttributeValues()
    {
        return AttributesBuilder.GetAttributeValues();
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        return AttributesBuilder.GetAttributeValues(attributeName);
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        return AttributesBuilder.GetAttributeLocales();
    }

    public IReferenceBuilder SetGroup(int primaryKey)
    {
        GroupId = primaryKey;
        return this;
    }

    public IReferenceBuilder SetGroup(string? referencedEntity, int primaryKey)
    {
        GroupType = referencedEntity;
        GroupId = primaryKey;
        return this;
    }

    public IReferenceBuilder RemoveGroup()
    {
        GroupId = null;
        return this;
    }

    public IReferenceBuilder RemoveAttribute(string attributeName)
    {
        AttributesBuilder.RemoveAttribute(attributeName);
        return this;
    }

    public IReferenceBuilder SetAttribute(string attributeName, object? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName);
        }

        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceKey.ReferenceName);
        AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, attributeName, attributeValue.GetType(), AttributesBuilder.GetLocationResolver());
        AttributesBuilder.SetAttribute(attributeName, attributeValue);
        return this;
    }

    public IReferenceBuilder SetAttribute(string attributeName, object[]? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName);
        }

        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceKey.ReferenceName);
        AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, attributeName, attributeValue.GetType(), AttributesBuilder.GetLocationResolver());
        AttributesBuilder.SetAttribute(attributeName, attributeValue);
        return this;
    }

    public IReferenceBuilder RemoveAttribute(string attributeName, CultureInfo locale)
    {
        AttributesBuilder.RemoveAttribute(attributeName, locale);
        return this;
    }

    public IReferenceBuilder SetAttribute(string attributeName, CultureInfo locale, object? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName, locale);
        }

        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceKey.ReferenceName);
        AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, attributeName, attributeValue.GetType(),
            locale, AttributesBuilder.GetLocationResolver());
        AttributesBuilder.SetAttribute(attributeName, locale, attributeValue);
        return this;
    }

    public IReferenceBuilder SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
    {
        if (attributeValue == null)
        {
            return RemoveAttribute(attributeName, locale);
        }

        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceKey.ReferenceName);
        AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, attributeName, attributeValue.GetType(),
            locale, AttributesBuilder.GetLocationResolver());
        AttributesBuilder.SetAttribute(attributeName, locale, attributeValue);
        return this;
    }

    public IReferenceBuilder MutateAttribute(AttributeMutation mutation)
    {
        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceKey.ReferenceName);
        AttributeVerificationUtils.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, mutation.AttributeKey.AttributeName, null, AttributesBuilder.GetLocationResolver());
        AttributesBuilder.MutateAttribute(mutation);
        return this;
    }

    public IEnumerable<ReferenceMutation> BuildChangeSet()
    {
        return new ReferenceMutation?[]
            {
                new InsertReferenceMutation(ReferenceKey, ReferenceCardinality, ReferencedEntityType),
                Group is null ? null : new SetReferenceGroupMutation(ReferenceKey, GroupType, GroupId!.Value)
            }
            .Where(x => x is not null)
            .Concat(
                AttributesBuilder.GetAttributeValues()
                    .Select(x =>
                        new ReferenceAttributeMutation(ReferenceKey, new UpsertAttributeMutation(x.Key, x.Value!)))
            )!;
    }

    public IReference Build()
    {
        return new Reference(
            EntitySchema,
            1,
            ReferenceKey.ReferenceName,
            ReferenceKey.PrimaryKey,
            ReferencedEntityType,
            ReferenceCardinality,
            Group,
            AttributesBuilder.Build()
        );
    }
}