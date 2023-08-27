using System.Globalization;
using Client.Models.Data;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;
using static Client.Models.Data.Structure.Attributes;

namespace Client.Models.Data.Structure;

public class Reference : IReference
{
    private IEntitySchema EntitySchema { get; }
    public int Version { get; }
    public ReferenceKey ReferenceKey { get; }
    public GroupEntityReference? Group { get; }
    public SealedEntity? GroupEntity { get; }
    private Attributes Attributes { get; }
    public bool Dropped { get; }
    public SealedEntity? ReferencedEntity { get; }
    public IReferenceSchema? ReferenceSchema => EntitySchema.GetReference(ReferenceKey.ReferenceName);
    public Cardinality? ReferenceCardinality => ReferenceSchema?.Cardinality ?? _referenceCardinality;
    public string? ReferencedEntityType => ReferenceSchema?.ReferencedEntityType ?? _referencedEntityType;
    public string ReferenceName => ReferenceKey.ReferenceName;
    public int ReferencedPrimaryKey => ReferenceKey.PrimaryKey; 
    public bool AttributesAvailable => Attributes.AttributesAvailable;
    private readonly Cardinality? _referenceCardinality;
    private readonly string? _referencedEntityType;
    
    public Reference(
		IEntitySchema entitySchema,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group,
		SealedEntity? referencedEntity = null,
		SealedEntity? groupEntity = null
	) {
		Version = 1;
		EntitySchema = entitySchema;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		_referenceCardinality = cardinality;
		_referencedEntityType = referencedEntityType;
		Group = group;
		Attributes = new Attributes(entitySchema);
		ReferencedEntity = referencedEntity;
		GroupEntity = groupEntity;
	}

	public Reference(
		IEntitySchema entitySchema,
		int version,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group,
		SealedEntity? referencedEntity = null,
		SealedEntity? groupEntity = null,
		bool dropped = false
	) {
		EntitySchema = entitySchema;
		Version = version;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		_referenceCardinality = cardinality;
		_referencedEntityType = referencedEntityType;
		Group = group;
		Attributes = new Attributes(entitySchema);
		ReferencedEntity = referencedEntity;
		GroupEntity = groupEntity;
		Dropped = dropped;
	}

	public Reference(
		IEntitySchema entitySchema,
		int version,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group,
		Attributes attributes,
		SealedEntity? referencedEntity = null,
		SealedEntity? groupEntity = null,
		bool dropped = false
	) {
		EntitySchema = entitySchema;
		Version = version;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		_referenceCardinality = cardinality;
		_referencedEntityType = referencedEntityType;
		Group = group;
		Attributes = attributes;
		ReferencedEntity = referencedEntity;
		GroupEntity = groupEntity;
		Dropped = dropped;
	}

	public Reference(
		IEntitySchema entitySchema,
		int version,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group,
		ICollection<AttributeValue> attributes,
		SealedEntity? referencedEntity = null,
		SealedEntity? groupEntity = null,
		bool dropped = false) 
	{
		EntitySchema = entitySchema;
		Version = version;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		_referenceCardinality = cardinality;
		_referencedEntityType = referencedEntityType;
		Group = group;
		Attributes = new Attributes(entitySchema, attributes);
		ReferencedEntity = referencedEntity;
		GroupEntity = groupEntity;
		Dropped = dropped;
	}

	public Reference(
		IEntitySchema entitySchema,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality cardinality,
		GroupEntityReference? group,
		Attributes attributes,
		SealedEntity? referencedEntity = null,
		SealedEntity? groupEntity = null,
		bool dropped = false
	)
	{
		EntitySchema = entitySchema;
		Version = 1;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		_referenceCardinality = cardinality;
		_referencedEntityType = referencedEntityType;
		Group = group;
		Attributes = attributes;
		ReferencedEntity = referencedEntity;
		GroupEntity = groupEntity;
		Dropped = dropped;
	}
	
	public object? GetAttribute(string attributeName)
	{
		return Attributes.GetAttribute(attributeName);
	}

	public object? GetAttribute(string attributeName, CultureInfo locale)
	{
		return Attributes.GetAttribute(attributeName, locale);
	}

	public object[]? GetAttributeArray(string attributeName)
	{
		return Attributes.GetAttributeArray(attributeName);
	}

	public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
	{
		return Attributes.GetAttributeArray(attributeName, locale);
	}

	public AttributeValue? GetAttributeValue(string attributeName)
	{
		return Attributes.GetAttributeValue(attributeName);
	}

	public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
	{
		return Attributes.GetAttributeValue(attributeName, locale);
	}

	public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
	{
		return Attributes.GetAttributeValue(attributeKey);
	}

	public IAttributeSchema GetAttributeSchema(string attributeName)
	{
		return Attributes.GetAttributeSchema(attributeName);
	}

	public ISet<string> GetAttributeNames()
	{
		return Attributes.GetAttributeNames();
	}

	public ISet<AttributeKey> GetAttributeKeys()
	{
		return Attributes.GetAttributeKeys();
	}

	public ICollection<AttributeValue> GetAttributeValues()
	{
		return Attributes.GetAttributeValues();
	}

	public ICollection<AttributeValue> GetAttributeValues(string attributeName)
	{
		return Attributes.GetAttributeValues(attributeName);
	}

	public ISet<CultureInfo> GetAttributeLocales()
	{
		return Attributes.GetAttributeLocales();
	}

	public override string ToString()
	{
		return (Dropped ? "❌ " : "") +
		       "References `" + ReferenceKey.ReferenceName + "` " + ReferenceKey.PrimaryKey +
		       (Group == null ? "" : " in " + Group) +
		       (Attributes.AttributesAvailable ? ", attrs: " + Attributes : "");
	}
}