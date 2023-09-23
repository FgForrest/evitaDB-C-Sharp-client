using System.Collections.Immutable;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class ReferenceSchema : IReferenceSchema
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
    public bool IsIndexed { get; }
    public bool IsFaceted { get; }
    public ICollection<IAttributeSchema> NonNullableAttributes { get; }
    public IDictionary<string, IAttributeSchema> Attributes { get; }
    private IDictionary<NamingConvention, string> EntityTypeNameVariants { get; }
    private IDictionary<NamingConvention, string> GroupTypeNameVariants { get; }
    private IDictionary<string, IAttributeSchema[]> AttributeNameIndex { get; }
    private IDictionary<string, SortableAttributeCompoundSchema> SortableAttributeCompounds { get; }
    private IDictionary<string, SortableAttributeCompoundSchema[]> SortableAttributeCompoundNameIndex { get; }
    private IDictionary<string, List<SortableAttributeCompoundSchema>> AttributeToSortableAttributeCompoundIndex { get; }

    internal static ReferenceSchema InternalBuild(
        string name,
        string entityType,
        bool entityTypeRelatesToEntity,
        Cardinality cardinality,
        string? groupType,
        bool groupTypeRelatesToEntity,
        bool indexed,
        bool faceted
    )
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, entityType);
        if (groupType != null)
        {
            ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, groupType);
        }

        if (faceted)
        {
            Assert.IsTrue(indexed, "When reference is marked as faceted, it needs also to be indexed.");
        }

        //we need to wrap even empty map to the unmodifiable wrapper in order to unify type for Kryo serialization
        return new ReferenceSchema(
            name, NamingConventionHelper.Generate(name),
            null, null, cardinality,
            entityType,
            entityTypeRelatesToEntity
                ? new Dictionary<NamingConvention, string>()
                : NamingConventionHelper.Generate(entityType),
            entityTypeRelatesToEntity,
            groupType,
            groupType != null && string.IsNullOrWhiteSpace(groupType) && !groupTypeRelatesToEntity
                ? NamingConventionHelper.Generate(groupType)
                : new Dictionary<NamingConvention, string>(),
            groupTypeRelatesToEntity,
            indexed,
            faceted,
            new Dictionary<string, IAttributeSchema>(),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );
    }

    /**
	 * This method is for internal purposes only. It could be used for reconstruction of ReferenceSchema from
	 * different package than current, but still internal code of the Evita ecosystems.
	 *
	 * Do not use this method from in the client code!
	 */
    internal static ReferenceSchema InternalBuild(
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
        IDictionary<string, IAttributeSchema> attributes,
        IDictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, entityType);
        if (groupType != null)
        {
            ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, groupType);
        }

        if (faceted)
        {
            Assert.IsTrue(indexed, "When reference is marked as faceted, it needs also to be indexed.");
        }

        //we need to wrap even empty map to the unmodifiable wrapper in order to unify type for Kryo serialization
        return new ReferenceSchema(
            name, NamingConventionHelper.Generate(name),
            description, deprecationNotice, cardinality,
            entityType,
            entityTypeRelatesToEntity
                ? new Dictionary<NamingConvention, string>()
                : NamingConventionHelper.Generate(entityType),
            entityTypeRelatesToEntity,
            groupType,
            groupType != null && string.IsNullOrWhiteSpace(groupType) && !groupTypeRelatesToEntity
                ? NamingConventionHelper.Generate(groupType)
                : new Dictionary<NamingConvention, string>(),
            groupTypeRelatesToEntity,
            indexed,
            faceted,
            attributes.ToImmutableDictionary(),
            sortableAttributeCompounds
        );
    }

    /**
	 * This method is for internal purposes only. It could be used for reconstruction of ReferenceSchema from
	 * different package than current, but still internal code of the Evita ecosystems.
	 *
	 * Do not use this method from in the client code!
	 */
    internal static ReferenceSchema InternalBuild(
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
        IDictionary<string, IAttributeSchema> attributes,
        IDictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, entityType);
        if (groupType != null)
        {
            ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, groupType);
        }

        if (faceted)
        {
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
            attributes.ToImmutableDictionary(),
            sortableAttributeCompounds
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
        bool indexed,
        bool faceted,
        IDictionary<string, IAttributeSchema> attributes,
        IDictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, referencedEntityType);
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
        IsIndexed = indexed;
        IsFaceted = faceted;
        Attributes = attributes.ToDictionary(x => x.Key, x => x.Value);
        AttributeNameIndex = EntitySchema.InternalGenerateNameVariantIndex(
            Attributes.Values, x => x.NameVariants
        );
        NonNullableAttributes = Attributes
            .Values
            .Where(x => !x.Nullable)
            .ToList();
        SortableAttributeCompounds = sortableAttributeCompounds.ToImmutableDictionary(
            x => x.Key,
            x => EntitySchema.ToSortableAttributeCompoundSchema(x.Value)
        );

        SortableAttributeCompoundNameIndex = EntitySchema.InternalGenerateNameVariantIndex(
            SortableAttributeCompounds.Values, x => x.NameVariants
        );

        AttributeToSortableAttributeCompoundIndex = SortableAttributeCompounds
            .Values
            .SelectMany(it => it.AttributeElements.Select(attribute => new AttributeToCompound(attribute, it)))
            .GroupBy(rec => rec.Attribute.AttributeName, compound => compound.CompoundSchema,
                (key, values) => new {Key = key, Values = values.ToList()})
            .ToDictionary(x => x.Key, x => x.Values);
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
            ? entitySchemaFetcher.Invoke(ReferencedGroupType!).NameVariants
            : GroupTypeNameVariants;
    }

    public string GetReferencedGroupTypeNameVariants(
        NamingConvention namingConvention,
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedGroupTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedGroupType!).GetNameVariant(namingConvention)
            : GroupTypeNameVariants[namingConvention];
    }

    public IDictionary<string, IAttributeSchema> GetAttributes()
    {
        return Attributes;
    }

    public IAttributeSchema? GetAttribute(string name)
    {
        return Attributes.TryGetValue(name, out var result) ? result : null;
    }

    public IAttributeSchema GetAttributeOrThrow(string name)
    {
        return Attributes.TryGetValue(name, out var result)
            ? result
            : throw new EvitaInvalidUsageException("Attribute `" + name + "` is not known in entity `" + Name +
                                                   "` schema!");
    }

    public IAttributeSchema? GetAttributeByName(string dataName, NamingConvention namingConvention)
    {
        return AttributeNameIndex.TryGetValue(dataName, out var result) ? result[(int) namingConvention] : null;
    }

    public string GetNameVariant(NamingConvention namingConvention) => NameVariants[namingConvention];

    public IDictionary<string, SortableAttributeCompoundSchema> GetSortableAttributeCompounds()
    {
        return SortableAttributeCompounds;
    }

    public SortableAttributeCompoundSchema? GetSortableAttributeCompound(string name)
    {
        return SortableAttributeCompounds.TryGetValue(name, out var result) ? result : null;
    }

    public SortableAttributeCompoundSchema? GetSortableAttributeCompoundByName(string name,
        NamingConvention namingConvention)
    {
        return SortableAttributeCompoundNameIndex.TryGetValue(name, out var result)
            ? result[(int) namingConvention]
            : null;
    }

    public IList<SortableAttributeCompoundSchema> GetSortableAttributeCompoundsForAttribute(string attributeName)
    {
        return AttributeToSortableAttributeCompoundIndex.TryGetValue(attributeName, out var result)
            ? result
            : new List<SortableAttributeCompoundSchema>();
    }

    public override string ToString()
    {
        return "ReferenceSchema{" +
               "name='" + Name + '\'' +
               ", cardinality=" + Cardinality +
               ", referencedEntityType=" + ReferencedEntityType +
               ", referencedGroupType=" + ReferencedGroupType +
               ", indexed=" + IsIndexed +
               ", faceted=" + IsFaceted +
               ", nonNullableAttributes=" + NonNullableAttributes +
               ", attributes=" + Attributes +
               '}';
    }

    private record AttributeToCompound(AttributeElement Attribute, SortableAttributeCompoundSchema CompoundSchema);
}