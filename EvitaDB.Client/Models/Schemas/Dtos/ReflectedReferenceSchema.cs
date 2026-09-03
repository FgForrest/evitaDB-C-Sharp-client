using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;
using Grpc.Core;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class ReflectedReferenceSchema : ReferenceSchema, IReflectedReferenceSchema
{
    public string ReflectedReferenceName { get; }
    public IReferenceSchema? ReflectedReference { get; }
    public bool DescriptionInherited { get; }
    public bool DeprecatedInherited { get; }
    public bool CardinalityInherited { get; }
    public bool FacetedInherited { get; }
    public AttributeInheritanceBehavior AttributeInheritanceBehavior { get; }
    public string[] AttributeInheritanceFilter { get; }
    public bool ReflectedReferenceAvailable { get; }
    public HashSet<string> InheritedAttributes { get; }

    internal static ReflectedReferenceSchema InternalBuild(
        string name,
        string entityType,
        string reflectedReferenceName
    )
    {
        return new ReflectedReferenceSchema(
            name, NamingConventionHelper.Generate(name),
            null, null, null, entityType, reflectedReferenceName,
            null, new Dictionary<string, IAttributeSchema>(),
            new Dictionary<string, SortableAttributeCompoundSchema>(),
            AttributeInheritanceBehavior.InheritOnlySpecified, [], null
        );
    }

    internal static ReflectedReferenceSchema InternalBuild(
        string name,
        string? description,
        string? deprecationNotice,
        string entityType,
        string reflectedReferenceName,
        Cardinality? cardinality,
        bool? faceted,
        Dictionary<string, IAttributeSchema> attributes,
        Dictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds,
        AttributeInheritanceBehavior attributesInherited,
        string[]? attributesExcludedFromInheritance
    )
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, entityType);
        return new ReflectedReferenceSchema(
            name, NamingConventionHelper.Generate(name),
            description, deprecationNotice, cardinality,
            entityType,
            reflectedReferenceName,
            faceted,
            attributes,
            sortableAttributeCompounds,
            attributesInherited,
            attributesExcludedFromInheritance ?? [],
            null
        );
    }

    public static ReflectedReferenceSchema InternalBuild(
        string name,
        Dictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        string entityType,
        string reflectedReferenceName,
        Cardinality? cardinality,
        bool? faceted,
        Dictionary<string, IAttributeSchema> attributes,
        Dictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds,
        AttributeInheritanceBehavior attributesInherited,
        string[]? attributesExcludedFromInheritance
    )
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, entityType);
        return new ReflectedReferenceSchema(
            name, nameVariants,
            description, deprecationNotice, cardinality,
            entityType,
            reflectedReferenceName,
            faceted,
            attributes,
            sortableAttributeCompounds,
            attributesInherited,
            attributesExcludedFromInheritance ?? [],
            null
        );
    }

    internal static ReflectedReferenceSchema InternalBuild(
        string name,
        Dictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        string entityType,
        Dictionary<NamingConvention, string?> entityTypeVariants,
        string referencedGroupType,
        Dictionary<NamingConvention, string?>? groupTypeVariants,
        bool referencedGroupManaged,
        string reflectedReferenceName,
        Cardinality? cardinality,
        bool? faceted,
        Dictionary<string, IAttributeSchema> attributes,
        Dictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds,
        bool descriptionInherited,
        bool deprecatedInherited,
        bool cardinalityInherited,
        bool facetedInherited,
        AttributeInheritanceBehavior attributesInherited,
        string[]? attributesExcludedFromInheritance,
        IReferenceSchema? reflectedReference
    )
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, entityType);
        return new ReflectedReferenceSchema(
            name, nameVariants,
            description, deprecationNotice, cardinality,
            entityType, entityTypeVariants,
            referencedGroupType, groupTypeVariants, referencedGroupManaged,
            reflectedReferenceName,
            faceted,
            attributes,
            sortableAttributeCompounds,
            descriptionInherited,
            deprecatedInherited,
            cardinalityInherited,
            facetedInherited,
            attributesInherited,
            attributesExcludedFromInheritance == null ? [] : attributesExcludedFromInheritance,
            reflectedReference
        );
    }

    public ReflectedReferenceSchema(
        string name,
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        Cardinality? cardinality,
        string referencedEntityType,
        string reflectedReferenceName,
        bool? faceted,
        Dictionary<string, IAttributeSchema> attributes,
        Dictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds,
        AttributeInheritanceBehavior attributesInheritanceBehavior,
        string[]? attributeInheritanceFilter,
        IReferenceSchema? reflectedReference
    ) : base(
        name, nameVariants,
        reflectedReference != null && description == null ? reflectedReference.Description : description,
        reflectedReference != null && deprecationNotice == null
            ? reflectedReference.DeprecationNotice
            : deprecationNotice,
        reflectedReference != null && cardinality == null ? reflectedReference.Cardinality : cardinality,
        referencedEntityType,
        new Dictionary<NamingConvention, string?>(), true,
        reflectedReference?.ReferencedGroupType,
        new Dictionary<NamingConvention, string?>(),
        reflectedReference is { ReferencedGroupTypeManaged: true },
        true,
        faceted ?? reflectedReference is { IsFaceted: true },
        reflectedReference == null
            ? attributes
            : Union(
                attributes,
                InvertNecessaryAttributeTypes(reflectedReference.GetAttributes()),
                attributesInheritanceBehavior,
                attributeInheritanceFilter ?? []
            ),
        reflectedReference == null
            ? sortableAttributeCompounds
            : Union(
                sortableAttributeCompounds,
                reflectedReference.GetSortableAttributeCompounds(),
                attributesInheritanceBehavior,
                attributeInheritanceFilter ?? []
            )
    )
    {
        Assert.IsTrue(
            reflectedReference == null || reflectedReference.Name.Equals(reflectedReferenceName),
            () =>
                $"Reflected reference name `{referencedEntityType}` must have the same name as the target reference (`{reflectedReference?.Name}`)!"
        );
        Assert.IsTrue(
            reflectedReference == null || reflectedReference.ReferencedEntityTypeManaged,
            () => $"Reflected reference name `{referencedEntityType}` must refer to a managed entity type!"
        );
        ReflectedReferenceName = reflectedReferenceName;
        ReflectedReference = reflectedReference;
        DescriptionInherited = description == null;
        DeprecatedInherited = deprecationNotice == null;
        CardinalityInherited = cardinality == null;
        Assert.IsTrue(
            CardinalityInherited || cardinality != null,
            "Cardinality must be either inherited or specified explicitly!"
        );
        FacetedInherited = faceted == null;
        Assert.IsTrue(
            FacetedInherited || faceted != null,
            "Faceted must be either inherited or specified explicitly!"
        );
        AttributeInheritanceBehavior = attributesInheritanceBehavior;
        AttributeInheritanceFilter = attributeInheritanceFilter == null ? [] : attributeInheritanceFilter;
        if (ReflectedReference == null)
        {
            InheritedAttributes = [];
        }
        else
        {
            switch (AttributeInheritanceBehavior)
            {
                case AttributeInheritanceBehavior.InheritOnlySpecified:
                {
                    InheritedAttributes = AttributeInheritanceFilter.ToHashSet();
                    InheritedAttributes.IntersectWith(ReflectedReference.GetAttributes().Keys);
                    break;
                }
                case AttributeInheritanceBehavior.InheritAllExcept:
                {
                    InheritedAttributes = new HashSet<string>(ReflectedReference.GetAttributes().Keys);
                    foreach (var b in AttributeInheritanceFilter)
                    {
                        InheritedAttributes.Remove(b);
                    }
                    break;
                }
                default:
                {
                    throw new EvitaInternalError(
                        "Unsupported attribute inheritance behavior: " + AttributeInheritanceBehavior
                    );
                }
            }
        }
    }

    private ReflectedReferenceSchema(
        string name,
        Dictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        Cardinality? cardinality,
        string referencedEntityType,
        Dictionary<NamingConvention, string?> entityTypeVariants,
        string referencedGroupType,
        IDictionary<NamingConvention, string?>? groupTypeVariants,
        bool referencedGroupManaged,
        string reflectedReferenceName,
        bool? faceted,
        Dictionary<string, IAttributeSchema> attributes,
        Dictionary<String, SortableAttributeCompoundSchema> sortableAttributeCompounds,
        bool descriptionInherited,
        bool deprecatedInherited,
        bool cardinalityInherited,
        bool facetedInherited,
        AttributeInheritanceBehavior attributesInheritanceBehavior,
        string[]? attributeInheritanceFilter,
        IReferenceSchema? reflectedReference
    ) : base(
        name, nameVariants,
        description,
        deprecationNotice,
        cardinality,
        referencedEntityType,
        entityTypeVariants,
        true,
        referencedGroupType,
        groupTypeVariants ?? new Dictionary<NamingConvention, string?>(),
        referencedGroupManaged,
        true,
        faceted.HasValue ? faceted.Value : true, // TODO tpz: fix after JNO corrects nullability
        attributes,
        sortableAttributeCompounds
        )
    {
        ReflectedReferenceName = reflectedReferenceName;
        ReflectedReference = reflectedReference;
        DescriptionInherited = descriptionInherited;
        DeprecatedInherited = deprecatedInherited;
        CardinalityInherited = cardinalityInherited;
        FacetedInherited = facetedInherited;
        AttributeInheritanceBehavior = attributesInheritanceBehavior;
        AttributeInheritanceFilter ??= [];
        if (ReflectedReference == null) 
        {
            InheritedAttributes = [];
        } else 
        {
            switch (AttributeInheritanceBehavior) 
            {
                case AttributeInheritanceBehavior.InheritOnlySpecified:
                {
                    InheritedAttributes = AttributeInheritanceFilter.ToHashSet();
                    InheritedAttributes.IntersectWith(ReflectedReference.GetAttributes().Keys);
                    break;
                }
                case AttributeInheritanceBehavior.InheritAllExcept:
                {
                    InheritedAttributes = ReflectedReference.GetAttributes().Keys.ToHashSet();
                    foreach (var b in AttributeInheritanceFilter)
                    {
                        InheritedAttributes.Remove(b);
                    }
                    break;
                }
                default: 
                    throw new EvitaInternalError(
                    "Unsupported attribute inheritance behavior: " + AttributeInheritanceBehavior
                );
            }
        }
    }

    private static Dictionary<string, TV> Union<TV>(
        Dictionary<string, TV> attributes,
        IDictionary<string, TV> reflectedAttributes,
        AttributeInheritanceBehavior attributesInheritanceBehavior,
        string[] attributeInheritanceFilter
    )
    {
        HashSet<String> filteredAttributes = new HashSet<string>(attributeInheritanceFilter);
        Dictionary<string, TV> result = new Dictionary<string, TV>(attributes);
        Predicate<string> attributeFilter =
            attributesInheritanceBehavior == AttributeInheritanceBehavior.InheritOnlySpecified
                ? filteredAttributes.Contains
                : attribute => !filteredAttributes.Contains(attribute);
        foreach (KeyValuePair<string, TV> reflectedEntry in reflectedAttributes)
        {
            if (attributeFilter.Invoke(reflectedEntry.Key))
            {
                Assert.IsPremiseValid(
                    !result.ContainsKey(reflectedEntry.Key),
                    "Attribute `" + reflectedEntry.Key + "` is inherited from the reflected reference, " +
                    "but it is already defined!"
                );
                result.Add(reflectedEntry.Key, reflectedEntry.Value);
            }
        }

        return result;
    }

    private static IDictionary<string, IAttributeSchema> InvertNecessaryAttributeTypes(
        IDictionary<string, IAttributeSchema> inputSchemas
    )
    {
        // we optimize for a scenario where no attribute schema needs inverted type
        Dictionary<String, IAttributeSchema>? invertedTypes = null;
        foreach (KeyValuePair<string, IAttributeSchema> entry in inputSchemas)
        {
            if (entry.Value.PlainType == typeof(Predecessor) ||
                entry.Value.PlainType == typeof(ReferencedEntityPredecessor))
            {
                {
                    invertedTypes = invertedTypes is null
                        ? new Dictionary<string, IAttributeSchema>(inputSchemas.Count)
                        : invertedTypes;
                    invertedTypes.Add(
                        entry.Key,
                        ((AttributeSchema)entry.Value).WithInvertedType()
                    );
                }
            }
        }

        if (invertedTypes != null)
        {
            // and we pay for it by second iteration
            foreach (KeyValuePair<string, IAttributeSchema> kvp in inputSchemas)
            {
                if (!invertedTypes.ContainsKey(kvp.Key))
                {
                    invertedTypes.Add(kvp.Key, kvp.Value);
                }
            }

            return invertedTypes;
        }

        return inputSchemas;
    }

    /// <summary>
    /// Returns a copy of this schema linked with the original reference schema it reflects. All inherited properties
    /// (description, deprecation notice, cardinality, faceted flag, attributes) are resolved against the original.
    /// </summary>
    public ReflectedReferenceSchema WithReferencedSchema(IReferenceSchema originalReference)
    {
        Assert.IsTrue(
            originalReference.IsIndexed,
            () => new InvalidSchemaMutationException(
                $"Referenced reference `{originalReference.Name}` must be indexed in order to propagate changes" +
                $" to reflected reference `{Name}`!"
            )
        );
        Dictionary<string, IAttributeSchema> declaredAttributes = GetAttributes()
            .Where(it => !InheritedAttributes.Contains(it.Key))
            .ToDictionary(it => it.Key, it => it.Value);
        Dictionary<string, SortableAttributeCompoundSchema> declaredSortableAttributeCompounds =
            GetSortableAttributeCompounds().ToDictionary(it => it.Key, it => it.Value);
        return new ReflectedReferenceSchema(
            Name, NameVariants,
            DescriptionInherited ? null : Description,
            DeprecatedInherited ? null : DeprecationNotice,
            CardinalityInherited ? null : Cardinality,
            ReferencedEntityType,
            ReflectedReferenceName,
            FacetedInherited ? null : IsFaceted,
            declaredAttributes,
            declaredSortableAttributeCompounds,
            AttributeInheritanceBehavior,
            AttributeInheritanceFilter,
            originalReference
        );
    }

    /// <summary>
    /// Equality of what was <em>declared</em>, not of what the declaration currently resolves to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The distinction is the whole point of this override. A reflected reference exists twice: as declared
    /// (<c>InternalBuild</c>) and as resolved against the reference it reflects
    /// (<see cref="WithReferencedSchema"/>, which fills in whatever is inherited). Those two describe the same
    /// declaration and must compare equal, because the one question anybody asks this method is
    /// <see cref="Mutations.References.CreateReflectedReferenceSchemaMutation"/>'s: <em>has this mutation
    /// already been applied?</em> — where the stored side is resolved and the incoming side is not.
    /// </para>
    /// <para>
    /// So the inherited-value getters are deliberately not compared: on a resolved instance they answer with
    /// the original's values and on a declared one with nothing, which made every redefinition of a reflected
    /// reference throw "already exists and has different definition". What is compared is the pair that is
    /// stable across resolution — the inheritance flags, and the values that are <em>not</em> inherited.
    /// </para>
    /// <para>
    /// The consequence of getting this wrong is not a subtly wrong answer: preparing a catalog twice, which
    /// every host restart does, failed outright, and a two-pass schema definition could not define a
    /// reference that reflected its own collection at all.
    /// </para>
    /// </remarks>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || GetType() != obj.GetType())
        {
            return false;
        }

        ReflectedReferenceSchema that = (ReflectedReferenceSchema)obj;

        return Name == that.Name
            && ReferencedEntityType == that.ReferencedEntityType
            && ReflectedReferenceName == that.ReflectedReferenceName
            && DescriptionInherited == that.DescriptionInherited
            && DeprecatedInherited == that.DeprecatedInherited
            && CardinalityInherited == that.CardinalityInherited
            && FacetedInherited == that.FacetedInherited
            && AttributeInheritanceBehavior == that.AttributeInheritanceBehavior
            && AttributeInheritanceFilter.SequenceEqual(that.AttributeInheritanceFilter)

            // Only where the value is the declaration's own. Where it is inherited both sides agree by
            // definition, and reading the getter would compare the original's value against nothing.
            && (DescriptionInherited || Description == that.Description)
            && (DeprecatedInherited || DeprecationNotice == that.DeprecationNotice)
            && (CardinalityInherited || Cardinality == that.Cardinality)
            && (FacetedInherited || IsFaceted == that.IsFaceted);
    }

    /// <remarks>
    /// Only fields <see cref="Equals"/> compares, and only ones stable across resolution - a hash that folded
    /// in an inherited value would disagree with equality. <see cref="AttributeInheritanceFilter"/> is an
    /// array, so it is folded in by length rather than by identity: an array's own hash is its reference,
    /// which would make two equal schemas hash differently and break every hashed lookup. It was previously
    /// counted twice while <see cref="AttributeInheritanceBehavior"/> was omitted.
    /// </remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name, ReferencedEntityType, ReflectedReferenceName, DescriptionInherited,
            DeprecatedInherited, CardinalityInherited, FacetedInherited,
            AttributeInheritanceFilter.Length);
    }
}
