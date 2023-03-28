using Client.Models.Data;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;
using static Client.Models.Data.Structure.Attributes;

namespace Client.Models.Data.Structure;

public class Reference
{
    private EntitySchema EntitySchema { get; }
    public int Version { get; }
    public ReferenceKey ReferenceKey { get; }
    private Cardinality? ReferenceCardinality { get; }
    private string? ReferencedEntityType { get; }
    private GroupEntityReference? Group { get; }
    private Attributes Attributes { get; }
    
    public Reference(
		EntitySchema entitySchema,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group
	) {
		Version = 1;
		EntitySchema = entitySchema;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		ReferenceCardinality = cardinality;
		ReferencedEntityType = referencedEntityType;
		Group = group;
		Attributes = new Attributes(entitySchema);
	}

	public Reference(
		EntitySchema entitySchema,
		int version,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group
	) {
		EntitySchema = entitySchema;
		Version = version;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		ReferenceCardinality = cardinality;
		ReferencedEntityType = referencedEntityType;
		Group = group;
		Attributes = new Attributes(entitySchema);
	}

	public Reference(
		EntitySchema entitySchema,
		int version,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group,
		Attributes attributes
	) {
		EntitySchema = entitySchema;
		Version = version;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		ReferenceCardinality = cardinality;
		ReferencedEntityType = referencedEntityType;
		Group = group;
		Attributes = attributes;
	}

	public Reference(
		EntitySchema entitySchema,
		int version,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality? cardinality,
		GroupEntityReference? group,
		ICollection<AttributeValue?> attributes) 
	{
		EntitySchema = entitySchema;
		Version = version;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		ReferenceCardinality = cardinality;
		ReferencedEntityType = referencedEntityType;
		Group = group;
		Attributes = new Attributes(entitySchema, attributes);
	}

	public Reference(
		EntitySchema entitySchema,
		string referenceName,
		int referencedEntityPrimaryKey,
		string? referencedEntityType,
		Cardinality cardinality,
		GroupEntityReference? group,
		Attributes attributes
	) {
		EntitySchema = entitySchema;
		Version = 1;
		ReferenceKey = new ReferenceKey(referenceName, referencedEntityPrimaryKey);
		ReferenceCardinality = cardinality;
		ReferencedEntityType = referencedEntityType;
		Group = group;
		Attributes = attributes;
	}

	public ReferenceSchema? GetReferenceSchema()
	{
		return EntitySchema.GetReference(ReferenceKey.ReferenceName);
	}
	
	public Cardinality? GetReferenceCardinality() {
		ReferenceSchema? schema = GetReferenceSchema();
		return schema?.Cardinality ?? ReferenceCardinality;
	}
	
	public string? GetReferencedEntityType() {
		ReferenceSchema? schema = GetReferenceSchema();
		return schema?.ReferencedEntityType ?? ReferencedEntityType;
	}

	public string ReferenceName => ReferenceKey.ReferenceName;
	
	public int ReferencedPrimaryKey => ReferenceKey.PrimaryKey; 

}