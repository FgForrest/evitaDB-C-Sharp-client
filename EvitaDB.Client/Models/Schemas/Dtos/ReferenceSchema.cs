using System.Collections.Immutable;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class ReferenceSchema : IReferenceSchema
{
    public string Name { get; }
    public IDictionary<NamingConvention, string?> NameVariants { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public Cardinality Cardinality { get; }
    public string ReferencedEntityType { get; }
    public string? ReferencedGroupType { get; }
    public bool ReferencedEntityTypeManaged { get; }
    public bool ReferencedGroupTypeManaged { get; }
    public bool IsIndexed { get; }
    public bool IsFaceted { get; }
    public ICollection<AttributeSchema> NonNullableAttributes { get; }
    public IDictionary<string, AttributeSchema> Attributes { get; }
    private IDictionary<NamingConvention, string?> EntityTypeNameVariants { get; }
    private IDictionary<NamingConvention, string?> GroupTypeNameVariants { get; }
    private IDictionary<string, AttributeSchema[]> AttributeNameIndex { get; }
    private IDictionary<string, SortableAttributeCompoundSchema> SortableAttributeCompounds { get; }
    private IDictionary<string, SortableAttributeCompoundSchema[]> SortableAttributeCompoundNameIndex { get; }

    private IDictionary<string, List<SortableAttributeCompoundSchema>> AttributeToSortableAttributeCompoundIndex
    {
        get;
    }

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
                ? new Dictionary<NamingConvention, string?>()
                : NamingConventionHelper.Generate(entityType),
            entityTypeRelatesToEntity,
            groupType,
            groupType != null && string.IsNullOrWhiteSpace(groupType) && !groupTypeRelatesToEntity
                ? NamingConventionHelper.Generate(groupType)
                : new Dictionary<NamingConvention, string?>(),
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
                ? new Dictionary<NamingConvention, string?>()
                : NamingConventionHelper.Generate(entityType),
            entityTypeRelatesToEntity,
            groupType,
            groupType != null && string.IsNullOrWhiteSpace(groupType) && !groupTypeRelatesToEntity
                ? NamingConventionHelper.Generate(groupType)
                : new Dictionary<NamingConvention, string?>(),
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
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        string entityType,
        IDictionary<NamingConvention, string?> entityTypeNameVariants,
        bool entityTypeRelatesToEntity,
        Cardinality cardinality,
        string? groupType,
        IDictionary<NamingConvention, string?>? groupTypeNameVariants,
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
            groupTypeNameVariants ?? new Dictionary<NamingConvention, string?>(),
            groupTypeRelatesToEntity,
            indexed,
            faceted,
            attributes.ToImmutableDictionary(),
            sortableAttributeCompounds
        );
    }

    protected ReferenceSchema(
        string name,
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        Cardinality? cardinality,
        string referencedEntityType,
        IDictionary<NamingConvention, string?> entityTypeNameVariants,
        bool referencedEntityTypeManaged,
        string? referencedGroupType,
        IDictionary<NamingConvention, string?> groupTypeNameVariants,
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
        Cardinality =
            cardinality ??
            Cardinality.ExactlyOne; // TODO tpz: after JNO fix this to correct value because of nullability problem
        ReferencedEntityType = referencedEntityType;
        EntityTypeNameVariants = entityTypeNameVariants;
        ReferencedEntityTypeManaged = referencedEntityTypeManaged;
        ReferencedGroupType = referencedGroupType;
        GroupTypeNameVariants = groupTypeNameVariants;
        ReferencedGroupTypeManaged = referencedGroupTypeManaged;
        IsIndexed = indexed;
        IsFaceted = faceted;
        Attributes = attributes.ToDictionary(x => x.Key, x => EntitySchema.ToReferenceAttributeSchema(x.Value));
        AttributeNameIndex = EntitySchema.InternalGenerateNameVariantIndex(
            Attributes.Values, x => x.NameVariants
        );
        NonNullableAttributes = Attributes
            .Values
            .Where(x => !x.Nullable())
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
                (key, values) => new { Key = key, Values = values.ToList() })
            .ToDictionary(x => x.Key, x => x.Values);
    }


    public IDictionary<NamingConvention, string?> GetEntityTypeNameVariants(
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedEntityTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedEntityType).NameVariants
            : EntityTypeNameVariants;
    }

    public string? GetReferencedEntityTypeNameVariants(
        NamingConvention namingConvention,
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedEntityTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedEntityType).GetNameVariant(namingConvention)
            : EntityTypeNameVariants[namingConvention];
    }

    public IDictionary<NamingConvention, string?> GetGroupTypeNameVariants(
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedGroupTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedGroupType!).NameVariants
            : GroupTypeNameVariants;
    }

    public string? GetReferencedGroupTypeNameVariants(
        NamingConvention namingConvention,
        Func<string, EntitySchema> entitySchemaFetcher)
    {
        return ReferencedGroupTypeManaged
            ? entitySchemaFetcher.Invoke(ReferencedGroupType!).GetNameVariant(namingConvention)
            : GroupTypeNameVariants[namingConvention];
    }

    public void Validate(ICatalogSchema catalogSchema, EntitySchema entitySchema)
    {
        IEntitySchema? referencedEntityTypeSchema = catalogSchema.GetEntitySchema(ReferencedEntityType);
        IEnumerable<string> referenceErrors = [];
        if (ReferencedEntityTypeManaged && referencedEntityTypeSchema is null)
        {
            referenceErrors = [..referenceErrors, "Referenced entity type `" + ReferencedEntityType + "` is not present in catalog `" + catalogSchema.Name + "` schema!"];
        }
        else if (!ReferencedEntityTypeManaged && referencedEntityTypeSchema is not null)
        {
            referenceErrors = [
                ..referenceErrors,
                "Referenced entity type `" + ReferencedEntityType + "` is present in catalog `" +
                catalogSchema.Name + "` schema, but it's marked as not managed!"
            ];
        }

        if (ReferencedGroupTypeManaged &&
            catalogSchema.GetEntitySchema(ReferencedGroupType!) is null
           )
        {
            referenceErrors = [
                ..referenceErrors,
                "Referenced group entity type `" + ReferencedGroupType! + "` is not present in catalog `" +
                          catalogSchema.Name + "` schema!"
            ];
        }
        else if (!ReferencedGroupTypeManaged &&
                 catalogSchema.GetEntitySchema(ReferencedGroupType!) is not null
                )
        {
            referenceErrors = [
                ..referenceErrors,
                "Referenced group entity type `" + ReferencedGroupType + "` is present in catalog `" +
                          catalogSchema.Name + "` schema, but it's marked as not managed!"
            ];
        }

        referenceErrors = [..referenceErrors, ..ValidateAttributes(GetAttributes())];

        List<string> errors = referenceErrors.Select(it => "\t" + it).ToList();
        if (errors.Count != 0)
        {
            throw new InvalidSchemaMutationException(
                "Reference schema `" + Name + "` contains validation errors:\n" + String.Join("\n", errors)
            );
        }
    }

    protected List<string> ValidateAttributes( IDictionary<string, IAttributeSchema> attributes)
    {
        List<string> attributeErrors = new List<string>();
        if (!this.IsIndexed)
        {
            foreach (IAttributeSchema attribute in attributes.Values)
            {
                if (attribute.Filterable())
                {
                    attributeErrors.Add(
                        $"Attribute `{attribute.Name}` of reference schema `{Name}` is filterable but reference schema is not indexed!");
                }

                if (attribute.Sortable())
                {
                    attributeErrors.Add(
                        $"Attribute `{attribute.Name}` of reference schema `{Name}` is sortable but reference schema is not indexed!");
                }

                if (attribute.Unique())
                {
                    attributeErrors.Add(
                        $"Attribute `{attribute.Name}` of reference schema `{Name}` is unique but reference schema is not indexed!");
                }
            }
        }

        return attributeErrors;
    }

    public IDictionary<string, IAttributeSchema> GetAttributes()
    {
        return Attributes.ToDictionary(x => x.Key, x => (IAttributeSchema)x.Value);
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
        return AttributeNameIndex.TryGetValue(dataName, out var result) ? result[(int)namingConvention] : null;
    }

    public string? GetNameVariant(NamingConvention namingConvention) =>
        NameVariants.TryGetValue(namingConvention, out string? name) ? name : null;

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
            ? result[(int)namingConvention]
            : null;
    }

    public IList<SortableAttributeCompoundSchema> GetSortableAttributeCompoundsForAttribute(string attributeName)
    {
        return AttributeToSortableAttributeCompoundIndex.TryGetValue(attributeName, out var result)
            ? result
            : new List<SortableAttributeCompoundSchema>();
    }

    /// <summary>
    /// Structural equality, mirroring the Java <c>ReferenceSchema.equals</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This was missing entirely, and the consequence was not a wrong comparison but a broken feature.</b>
    /// <see cref="Mutations.References.CreateReflectedReferenceSchemaMutation"/> compares an existing
    /// reference schema with the one a mutation would produce and treats equality as "already applied, nothing
    /// to do". <see cref="ReflectedReferenceSchema.Equals"/> delegates here through <c>base.Equals</c> — which,
    /// without this override, was <see cref="object"/>'s reference equality and therefore false for two
    /// separately-built instances describing the very same reference. So that no-op path was unreachable and
    /// defining a reflected reference a second time always threw "already exists and has different definition".
    /// </para>
    /// <para>
    /// That is invisible until something defines a schema twice, which is exactly what an idempotent
    /// catalog preparation does. The platform's two-pass definition — needed because reflected references
    /// point at collections that may not exist yet — hit it the moment a reference reflected its <em>own</em>
    /// collection and so could be created on the first pass as well as the second.
    /// </para>
    /// <para>
    /// The Java field set is followed rather than the C# property set where they differ: this port models
    /// indexed and faceted as plain booleans instead of Java's per-scope collections, so those two stand in
    /// for the six scope collections Java compares.
    /// </para>
    /// </remarks>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        // GetType rather than `is`, so a reflected reference is never equal to a plain one that happens to
        // carry the same values - the subclass compares more, and Java draws the line in the same place.
        if (obj is null || GetType() != obj.GetType())
        {
            return false;
        }

        ReferenceSchema that = (ReferenceSchema)obj;

        return ReferencedEntityTypeManaged == that.ReferencedEntityTypeManaged
            && ReferencedGroupTypeManaged == that.ReferencedGroupTypeManaged
            && IsIndexed == that.IsIndexed
            && IsFaceted == that.IsFaceted
            && Name == that.Name
            && Description == that.Description
            && DeprecationNotice == that.DeprecationNotice
            && Cardinality == that.Cardinality
            && ReferencedEntityType == that.ReferencedEntityType
            && ReferencedGroupType == that.ReferencedGroupType
            && SameKeyedValues(NameVariants, that.NameVariants)
            && SameKeyedValues(EntityTypeNameVariants, that.EntityTypeNameVariants)
            && SameKeyedValues(GroupTypeNameVariants, that.GroupTypeNameVariants)
            && SameAttributes(Attributes, that.Attributes)
            && SameKeys(SortableAttributeCompounds, that.SortableAttributeCompounds);
    }

    public override int GetHashCode() =>
        HashCode.Combine(
            Name,
            Cardinality,
            ReferencedEntityType,
            ReferencedGroupType,
            ReferencedEntityTypeManaged,
            ReferencedGroupTypeManaged,
            IsIndexed,
            IsFaceted);

    /// <summary>Two dictionaries hold the same keys mapped to the same values, in any order.</summary>
    private static bool SameKeyedValues<TKey, TValue>(
        IDictionary<TKey, TValue> left,
        IDictionary<TKey, TValue> right)
        where TKey : notnull =>
        left.Count == right.Count
        && left.All(it => right.TryGetValue(it.Key, out TValue? other)
            && EqualityComparer<TValue>.Default.Equals(it.Value, other));

    /// <summary>
    /// Two attribute maps describe the same attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Compared field by field rather than with <see cref="object.Equals(object)"/>, because
    /// <see cref="AttributeSchema"/> does not override it and would compare by reference — which is the very
    /// bug this file exists to fix, one level down.
    /// </para>
    /// <para>
    /// <b>Comparing only the names is not good enough</b>, and the way that fails is worth recording: callers
    /// treat equality as "nothing changed, skip the update", so a reference whose attribute merely changed
    /// from non-filterable to filterable would compare equal and the change would be silently dropped.
    /// </para>
    /// </remarks>
    private static bool SameAttributes(
        IDictionary<string, AttributeSchema> left,
        IDictionary<string, AttributeSchema> right) =>
        left.Count == right.Count
        && left.All(it => right.TryGetValue(it.Key, out AttributeSchema? other) && SameAttribute(it.Value, other));

    private static bool SameAttribute(IAttributeSchema left, IAttributeSchema right) =>
        left.Name == right.Name
        && left.Type == right.Type
        && left.Description == right.Description
        && left.DeprecationNotice == right.DeprecationNotice
        && left.Unique() == right.Unique()
        && left.UniqueWithinLocale() == right.UniqueWithinLocale()
        && left.Filterable() == right.Filterable()
        && left.Sortable() == right.Sortable()
        && left.Localized() == right.Localized()
        && left.Nullable() == right.Nullable()
        && left.IndexedDecimalPlaces == right.IndexedDecimalPlaces
        && Equals(left.DefaultValue, right.DefaultValue);

    /// <summary>
    /// Two dictionaries hold the same keys.
    /// </summary>
    /// <remarks>
    /// Names only, for sortable attribute compounds: their schema type has no structural equality either, and
    /// unlike an attribute it carries nothing a mutation edits in place — a compound is added or removed.
    /// </remarks>
    private static bool SameKeys<TValue>(
        IDictionary<string, TValue> left,
        IDictionary<string, TValue> right) =>
        left.Count == right.Count && left.Keys.All(right.ContainsKey);

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
