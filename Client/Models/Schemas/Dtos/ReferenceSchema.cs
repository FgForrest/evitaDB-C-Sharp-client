using System.Collections.Immutable;
using Client.Exceptions;
using Client.Utils;

namespace Client.Models.Schemas.Dtos;

public class ReferenceSchema
{
    public string Name { get; }
    public IDictionary<NamingConvention, string> NameVariants { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public Cardinality Cardinality { get; }
    public string ReferencedEntityType { get; }
    public string? ReferencedGroupType { get; }
    public bool ReferencedEntityTypeManaged { get; }
    public bool ReferencedGroupTypeManaged { get; }
    public bool Filterable { get; }
    public bool Faceted { get; }
    public ICollection<AttributeSchema> NonNullableAttributes { get; }
    public IDictionary<string, AttributeSchema> Attributes { get; }

    private IDictionary<NamingConvention, string> EntityTypeNameVariants { get; }
    private IDictionary<NamingConvention, string> GroupTypeNameVariants { get; }
    private IDictionary<string, AttributeSchema[]> AttributeNameIndex { get; }

    public static ReferenceSchema InternalBuild(
		string name,
		string entityType,
		bool entityTypeRelatesToEntity,
		Cardinality cardinality,
		string? groupType,
		bool groupTypeRelatesToEntity,
		bool indexed,
		bool faceted
	) {
		//ClassifierUtils.validateClassifierFormat(ClassifierType.ENTITY, entityType);
		if (groupType != null) {
			//ClassifierUtils.validateClassifierFormat(ClassifierType.ENTITY, groupType);
		}
		if (faceted) {
			Assert.IsTrue(indexed, "When reference is marked as faceted, it needs also to be indexed.");
		}

		//we need to wrap even empty map to the unmodifiable wrapper in order to unify type for Kryo serialization
		//noinspection RedundantUnmodifiable
		return new ReferenceSchema(
			name, NamingConventionHelper.Generate(name),
			null, null, cardinality,
			entityType,
			entityTypeRelatesToEntity ? new Dictionary<NamingConvention, string>() : NamingConventionHelper.Generate(entityType),
			entityTypeRelatesToEntity,
			groupType,
			groupType != null && string.IsNullOrWhiteSpace(groupType) && !groupTypeRelatesToEntity ?
				NamingConventionHelper.Generate(groupType) : new Dictionary<NamingConvention, string>(),
			groupTypeRelatesToEntity,
			indexed,
			faceted,
			new Dictionary<string, AttributeSchema>().ToImmutableDictionary()
		);
	}

	/**
	 * This method is for internal purposes only. It could be used for reconstruction of ReferenceSchema from
	 * different package than current, but still internal code of the Evita ecosystems.
	 *
	 * Do not use this method from in the client code!
	 */
	public static ReferenceSchema InternalBuild(
		string name,
		string? description,
		string? deprecationNotice,
		string entityType,
		bool entityTypeRelatesToEntity,
		Cardinality cardinality,
		string? groupType,
		bool groupTypeRelatesToEntity,
		bool indexed,
		bool faceted,
		Dictionary<string, AttributeSchema> attributes
	) {
		//ClassifierUtils.validateClassifierFormat(ClassifierType.ENTITY, entityType);
		if (groupType != null) {
			//ClassifierUtils.validateClassifierFormat(ClassifierType.ENTITY, groupType);
		}
		if (faceted) {
			Assert.IsTrue(indexed, "When reference is marked as faceted, it needs also to be indexed.");
		}

		//we need to wrap even empty map to the unmodifiable wrapper in order to unify type for Kryo serialization
		return new ReferenceSchema(
			name, NamingConventionHelper.Generate(name),
			description, deprecationNotice, cardinality,
			entityType,
			entityTypeRelatesToEntity ? new Dictionary<NamingConvention, string>() : NamingConventionHelper.Generate(entityType),
			entityTypeRelatesToEntity,
			groupType,
			groupType != null && string.IsNullOrWhiteSpace(groupType) && !groupTypeRelatesToEntity ?
				NamingConventionHelper.Generate(groupType) : new Dictionary<NamingConvention, string>(),
			groupTypeRelatesToEntity,
			indexed,
			faceted,
			attributes.ToImmutableDictionary()
		);
	}

	/**
	 * This method is for internal purposes only. It could be used for reconstruction of ReferenceSchema from
	 * different package than current, but still internal code of the Evita ecosystems.
	 *
	 * Do not use this method from in the client code!
	 */
	public static ReferenceSchema InternalBuild(
		string name,
		IDictionary<NamingConvention, string> nameVariants,
		string? description,
		string? deprecationNotice,
		string entityType,
		IDictionary<NamingConvention, string> entityTypeNameVariants,
		bool entityTypeRelatesToEntity,
		Cardinality cardinality,
		string? groupType,
		IDictionary<NamingConvention, string>? groupTypeNameVariants,
		bool groupTypeRelatesToEntity,
		bool indexed,
		bool faceted,
		IDictionary<string, AttributeSchema> attributes
	) {
		//ClassifierUtils.validateClassifierFormat(ClassifierType.ENTITY, entityType);
		if (groupType != null) {
			//ClassifierUtils.validateClassifierFormat(ClassifierType.ENTITY, groupType);
		}
		if (faceted) {
			Assert.IsTrue(indexed, "When reference is marked as faceted, it needs also to be indexed.");
		}

		//we need to wrap even empty map to the unmodifiable wrapper in order to unify type for Kryo serialization
		return new ReferenceSchema(
			name, nameVariants,
			description, deprecationNotice, cardinality,
			entityType,
			entityTypeNameVariants,
			entityTypeRelatesToEntity,
			groupType,
			groupTypeNameVariants ?? new Dictionary<NamingConvention, string>(),
			groupTypeRelatesToEntity,
			indexed,
			faceted,
			attributes.ToImmutableDictionary()
		);
	}
    
    private ReferenceSchema(
        string name,
        IDictionary<NamingConvention, string> nameVariants,
        string? description,
        string? deprecationNotice,
        Cardinality cardinality,
        string referencedEntityType,
        IDictionary<NamingConvention, string> entityTypeNameVariants,
        bool referencedEntityTypeManaged,
        string? referencedGroupType,
        IDictionary<NamingConvention, string> groupTypeNameVariants,
        bool referencedGroupTypeManaged,
        bool filterable,
        bool faceted,
        IDictionary<string, AttributeSchema> attributes
    )
    {
        //TODO: ClassifierUtils.validateClassifierFormat(ClassifierType.ENTITY, referencedEntityType);
        Name = name;
        NameVariants = nameVariants;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Cardinality = cardinality;
        ReferencedEntityType = referencedEntityType;
        EntityTypeNameVariants = entityTypeNameVariants;
        ReferencedEntityTypeManaged = referencedEntityTypeManaged;
        ReferencedGroupType = referencedGroupType;
        GroupTypeNameVariants = groupTypeNameVariants;
        ReferencedGroupTypeManaged = referencedGroupTypeManaged;
        Filterable = filterable;
        Faceted = faceted;
        Attributes = attributes.ToDictionary(x => x.Key, x => x.Value);
        AttributeNameIndex = EntitySchema.InternalGenerateNameVariantIndex(
            Attributes.Values, x => x.NameVariants
        );
        NonNullableAttributes = Attributes
            .Values
            .Where(x => !x.Nullable)
            .ToList();
    }


    public IDictionary<NamingConvention, string> GetEntityTypeNameVariants(
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedEntityTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedEntityType).NameVariants
            : EntityTypeNameVariants;
    }

    public string GetReferencedEntityTypeNameVariants(
        NamingConvention namingConvention,
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedEntityTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedEntityType).GetNameVariant(namingConvention)
            : EntityTypeNameVariants[namingConvention];
    }

    public IDictionary<NamingConvention, string> GetGroupTypeNameVariants(
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedGroupTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedGroupType).NameVariants
            : GroupTypeNameVariants;
    }

    public string GetReferencedGroupTypeNameVariants(
        NamingConvention namingConvention,
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedGroupTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedGroupType).GetNameVariant(namingConvention)
            : GroupTypeNameVariants[namingConvention];
    }

    public AttributeSchema? GetAttribute(string name)
    {
        return Attributes.TryGetValue(name, out var result) ? result : null;
    }

    public AttributeSchema GetAttributeOrThrow(string name)
    {
        return Attributes.TryGetValue(name, out var result)
            ? result
            : throw new EvitaInvalidUsageException("Attribute `" + name + "` is not known in entity `" + Name +
                                                   "` schema!");
    }

    public AttributeSchema? GetAttributeByName(string dataName, NamingConvention namingConvention)
    {
        return AttributeNameIndex.TryGetValue(dataName, out var result) ? result[(int) namingConvention] : null;
    }

    public string GetNameVariant(NamingConvention namingConvention) => NameVariants[namingConvention];
}