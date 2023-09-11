using System.Globalization;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Mutations.Reference;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Builder that is used to alter existing <see cref="Reference"/>.
/// </summary>
public class ExistingReferenceBuilder : IReferenceBuilder
{
    private IReference BaseReference { get; }
    private IEntitySchema EntitySchema { get; }
    private ExistingAttributesBuilder AttributesBuilder { get; }
    private ReferenceMutation? ReferenceGroupMutation { get; set; }
    public int Version => BaseReference.Version;
    public bool Dropped => BaseReference.Dropped;
    public ReferenceKey ReferenceKey => BaseReference.ReferenceKey;

    public GroupEntityReference? Group
    {
        get
        {
            GroupEntityReference? group = BaseReference.Group;

            if (group is not null)
            {
                if (ReferenceGroupMutation is not null)
                {
                    var mutatedReference = ReferenceGroupMutation.MutateLocal(EntitySchema, BaseReference);
                    group = mutatedReference.Group ?? group;
                }
            }
            else
            {
                if (ReferenceGroupMutation is not null)
                {
                    var mutatedReference = ReferenceGroupMutation.MutateLocal(EntitySchema, BaseReference);
                    group = mutatedReference.Group;
                }
            }

            return group is not null && !group.Dropped ? group : null;
            
        }
    }
    public ISealedEntity? GroupEntity => Group?.PrimaryKey == BaseReference.Group?.PrimaryKey ? BaseReference.GroupEntity : null;
    public ISealedEntity? ReferencedEntity => BaseReference.ReferencedEntity;
    public IReferenceSchema? ReferenceSchema => EntitySchema.GetReference(BaseReference.ReferenceName);
    public Cardinality? ReferenceCardinality => BaseReference.ReferenceCardinality;
    public string? ReferencedEntityType => BaseReference.ReferencedEntityType;
    public string ReferenceName => BaseReference.ReferenceName;
    public int ReferencedPrimaryKey => BaseReference.ReferencedPrimaryKey;

    public ExistingReferenceBuilder(IReference reference, IEntitySchema entitySchema)
    {
        BaseReference = reference;
        EntitySchema = entitySchema;
        AttributesBuilder = new ExistingAttributesBuilder(
            entitySchema,
            BaseReference.ReferenceSchema,
            BaseReference.GetAttributeValues(),
            BaseReference.ReferenceSchema?.GetAttributes() ?? new Dictionary<string, IAttributeSchema>(),
            true
        );
    }

    public IReferenceBuilder RemoveAttribute(string attributeName)
    {
        AttributesBuilder.RemoveAttribute(attributeName);
        return this;
    }

    public IReferenceBuilder SetAttribute(string attributeName, object? attributeValue)
    {
        if (attributeValue == null) {
            return RemoveAttribute(attributeName);
        }

        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceName);
        InitialReferenceBuilder.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, attributeName, attributeValue.GetType());
        AttributesBuilder.SetAttribute(attributeName, attributeValue);
        return this;
    }

    public IReferenceBuilder SetAttribute(string attributeName, object[]? attributeValue)
    {
        if (attributeValue == null) {
            return RemoveAttribute(attributeName);
        }

        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceName);
        InitialReferenceBuilder.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, attributeName, attributeValue.GetType());
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
        if (attributeValue == null) {
            return RemoveAttribute(attributeName, locale);
        }

        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceName);
        InitialReferenceBuilder.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, attributeName, attributeValue.GetType(), locale);
        AttributesBuilder.SetAttribute(attributeName, locale, attributeValue);
        return this;
    }

    public IReferenceBuilder SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
    {
        if (attributeValue == null) {
            return RemoveAttribute(attributeName, locale);
        }

        IReferenceSchema? referenceSchema = EntitySchema.GetReference(ReferenceName);
        InitialReferenceBuilder.VerifyAttributeIsInSchemaAndTypeMatch(EntitySchema, referenceSchema, attributeName, attributeValue.GetType(), locale);
        AttributesBuilder.SetAttribute(attributeName, locale, attributeValue);
        return this;
    }

    public IReferenceBuilder MutateAttribute(AttributeMutation mutation)
    {
        AttributesBuilder.MutateAttribute(mutation);
        return this;
    }

    public IReferenceBuilder SetGroup(int primaryKey)
    {
        ReferenceGroupMutation = new SetReferenceGroupMutation(
            BaseReference.ReferenceKey,
            null, primaryKey
        );
        return this;
    }

    public IReferenceBuilder SetGroup(string? referencedEntity, int primaryKey)
    {
        ReferenceGroupMutation = new SetReferenceGroupMutation(
            BaseReference.ReferenceKey,
            referencedEntity, primaryKey
        );
        return this;
    }

    public IReferenceBuilder RemoveGroup()
    {
        ReferenceGroupMutation = new RemoveReferenceGroupMutation(
            BaseReference.ReferenceKey
        );
        return this;
    }

    public IEnumerable<ReferenceMutation> BuildChangeSet()
    {
        AtomicReference<IReference> builtReference = new AtomicReference<IReference>(BaseReference);
        List<ReferenceMutation> referenceMutations = new List<ReferenceMutation>();
        if (ReferenceGroupMutation is not null)
        {
            IReference? existingValue = builtReference.Value;
            IReference newReference = ReferenceGroupMutation.MutateLocal(EntitySchema, existingValue);
            builtReference.Value = newReference;
            if (existingValue == null || newReference.Version > existingValue.Version)
            {
                referenceMutations.Add(ReferenceGroupMutation);
            }
        }

        referenceMutations.AddRange(
            AttributesBuilder
                .BuildChangeSet()
                .Select(it =>
                    new ReferenceAttributeMutation(
                        BaseReference.ReferenceKey,
                        it
                    )
                )
        );
        return referenceMutations;
    }

    public IReference Build()
    {
        GroupEntityReference? newGroup = Group;
        Attributes newAttributes = AttributesBuilder.Build();
        bool groupDiffers = BaseReference.Group?.DiffersFrom(newGroup) ?? newGroup is not null;

        if (groupDiffers || AttributesBuilder.AnyChangeInMutations())
        {
            return new Reference(
                EntitySchema,
                Version + 1,
                ReferenceName, ReferencedPrimaryKey,
                ReferencedEntityType, ReferenceCardinality,
                newGroup,
                newAttributes
            );
        }

        return BaseReference;
    }

    public bool AttributesAvailable()
    {
        return ((IAttributes) AttributesBuilder).AttributesAvailable();
    }

    public bool AttributesAvailable(CultureInfo locale)
    {
        return ((IAttributes) AttributesBuilder).AttributesAvailable(locale);
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
}