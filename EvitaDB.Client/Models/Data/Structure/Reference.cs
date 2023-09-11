using System.Globalization;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas;
using static EvitaDB.Client.Models.Data.Structure.Attributes;

namespace EvitaDB.Client.Models.Data.Structure;

public class Reference : IReference
{
    private IEntitySchema EntitySchema { get; }
    public int Version { get; }
    public ReferenceKey ReferenceKey { get; }
    public GroupEntityReference? Group { get; }
    public ISealedEntity? GroupEntity { get; }
    private Attributes Attributes { get; }
    public bool Dropped { get; }
    public ISealedEntity? ReferencedEntity { get; }
    public IReferenceSchema? ReferenceSchema => EntitySchema.GetReference(ReferenceKey.ReferenceName);
    public Cardinality? ReferenceCardinality => ReferenceSchema?.Cardinality ?? _referenceCardinality;
    public string? ReferencedEntityType => ReferenceSchema?.ReferencedEntityType ?? _referencedEntityType;
    public string ReferenceName => ReferenceKey.ReferenceName;
    public int ReferencedPrimaryKey => ReferenceKey.PrimaryKey; 
    public bool AttributesAvailable() => Attributes.AttributesAvailable();
    public bool AttributesAvailable(CultureInfo locale) => Attributes.AttributesAvailable(locale);
    public bool AttributeAvailable(string attributeName) => Attributes.AttributeAvailable(attributeName);
    public bool AttributeAvailable(string attributeName, CultureInfo locale) => Attributes.AttributeAvailable(attributeName, locale);

    private readonly Cardinality? _referenceCardinality;
    private readonly string? _referencedEntityType;
    
    public Reference(
		IEntitySchema entitySchema,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group,
		ISealedEntity? referencedEntity = null,
		ISealedEntity? groupEntity = null
	) {
		Version = 1;
		EntitySchema = entitySchema;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		_referenceCardinality = cardinality;
		_referencedEntityType = referencedEntityType;
		Group = group;
		IReferenceSchema? referenceSchema = entitySchema.GetReference(referenceName);
		Attributes = new Attributes(
			entitySchema, 
			referenceSchema ?? CreateImplicitSchema(referenceName, referencedEntityType, cardinality, group),
			new List<AttributeValue>(),
			referenceSchema is not null ? referenceSchema.GetAttributes() : new Dictionary<string, IAttributeSchema>()
		);
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
		ISealedEntity? referencedEntity = null,
		ISealedEntity? groupEntity = null,
		bool dropped = false
	) {
		EntitySchema = entitySchema;
		Version = version;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		_referenceCardinality = cardinality;
		_referencedEntityType = referencedEntityType;
		Group = group;
		IReferenceSchema? referenceSchema = entitySchema.GetReference(referenceName);
		Attributes = new Attributes(
			entitySchema, 
			referenceSchema ?? CreateImplicitSchema(referenceName, referencedEntityType, cardinality, group),
			new List<AttributeValue>(),
			referenceSchema is not null ? referenceSchema.GetAttributes() : new Dictionary<string, IAttributeSchema>()
		);
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
		ISealedEntity? referencedEntity = null,
		ISealedEntity? groupEntity = null,
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
		ISealedEntity? referencedEntity = null,
		ISealedEntity? groupEntity = null,
		bool dropped = false) 
	{
		EntitySchema = entitySchema;
		Version = version;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		_referenceCardinality = cardinality;
		_referencedEntityType = referencedEntityType;
		Group = group;
		IReferenceSchema? referenceSchema = entitySchema.GetReference(referenceName);
		Attributes = new Attributes(
			entitySchema, 
			referenceSchema ?? CreateImplicitSchema(referenceName, referencedEntityType, cardinality, group),
			attributes,
			referenceSchema is not null ? referenceSchema.GetAttributes() : new Dictionary<string, IAttributeSchema>()
		);
		ReferencedEntity = referencedEntity;
		GroupEntity = groupEntity;
		Dropped = dropped;
	}

	public Reference(
		IEntitySchema entitySchema,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group,
		Attributes attributes,
		ISealedEntity? referencedEntity = null,
		ISealedEntity? groupEntity = null,
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
	
	public static ReferenceSchema CreateImplicitSchema(
		string referenceName,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group
	) {
		return Schemas.Dtos.ReferenceSchema.InternalBuild(
			referenceName, referencedEntityType!, false, cardinality!.Value,
			group?.Type, false,
			false, false
		);
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

	public IAttributeSchema? GetAttributeSchema(string attributeName)
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
		       (Attributes.AttributesAvailable() ? ", attrs: " + Attributes : "");
	}
}