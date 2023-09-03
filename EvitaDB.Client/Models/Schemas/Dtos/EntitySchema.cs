using System.Collections.Immutable;
using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class EntitySchema : IEntitySchema
{
    public int Version { get; }
    public string Name { get; }
    public IDictionary<NamingConvention, string> NameVariants { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public bool WithGeneratedPrimaryKey { get; }
    public bool WithHierarchy { get; }
    public bool WithPrice { get; }
    public int IndexedPricePlaces { get; }
    public ISet<CultureInfo> Locales { get; }
    public ISet<Currency> Currencies { get; }
    public ISet<EvolutionMode> EvolutionModes { get; }
    public IEnumerable<IAttributeSchema> NonNullableAttributes { get; }
    public IEnumerable<IAssociatedDataSchema> NonNullableAssociatedData { get; }
    public IDictionary<string, IAttributeSchema> Attributes { get; }
    private IDictionary<string, IAttributeSchema[]> AttributeNameIndex { get; }
    private IDictionary<string, SortableAttributeCompoundSchema> SortableAttributeCompounds { get; }
    private IDictionary<string, SortableAttributeCompoundSchema[]> SortableAttributeCompoundNameIndex { get; }
    private IDictionary<string, List<SortableAttributeCompoundSchema>> AttributeToSortableAttributeCompoundIndex { get; }
    public IDictionary<string, IAssociatedDataSchema> AssociatedData { get; }
    private IDictionary<string, IAssociatedDataSchema[]> AssociatedDataNameIndex { get; }
    public IDictionary<string, IReferenceSchema> References { get; }
    private IDictionary<string, IReferenceSchema[]> ReferenceNameIndex { get; }

    private EntitySchema(
        int version,
        string name,
        IDictionary<NamingConvention, string> nameVariants,
        string? description,
        string? deprecationNotice,
        bool withGeneratedPrimaryKey,
        bool withHierarchy,
        bool withPrice,
        int indexedPricePlaces,
        ISet<CultureInfo> locales,
        ISet<Currency> currencies,
        IDictionary<string, IAttributeSchema> attributes,
        IDictionary<string, IAssociatedDataSchema> associatedData,
        IDictionary<string, IReferenceSchema> references,
        ISet<EvolutionMode> evolutionMode,
        IDictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds)
    {
        Version = version;
        Name = name;
        NameVariants = nameVariants;
        Description = description;
        DeprecationNotice = deprecationNotice;
        WithGeneratedPrimaryKey = withGeneratedPrimaryKey;
        WithHierarchy = withHierarchy;
        WithPrice = withPrice;
        IndexedPricePlaces = indexedPricePlaces;
        Locales = locales;
        Currencies = currencies;
        Attributes = attributes.ToDictionary(x => x.Key, x => x.Value);
        AttributeNameIndex =
            InternalGenerateNameVariantIndex(Attributes.Values, x => x.NameVariants);
        AssociatedData = associatedData.ToDictionary(x => x.Key, x => x.Value);
        AssociatedDataNameIndex =
            InternalGenerateNameVariantIndex(AssociatedData.Values, x => x.NameVariants);
        References = references.ToDictionary(x => x.Key, x => x.Value);
        ReferenceNameIndex =
            InternalGenerateNameVariantIndex(References.Values, x => x.NameVariants);
        EvolutionModes = evolutionMode;
        NonNullableAttributes = this.Attributes
            .Values
            .Where(it => !it.Nullable)
            .ToList();
        NonNullableAssociatedData = AssociatedData
            .Values
            .Where(it => !it.Nullable)
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

    public bool IsBlank()
    {
        return Version == 1 && !WithGeneratedPrimaryKey && !WithHierarchy && !WithPrice && IndexedPricePlaces == 2 &&
               !Locales.Any() && !Currencies.Any() && !Attributes.Any() && !AssociatedData.Any() && !References.Any()
               && EvolutionModes.Count == Enum.GetValues<EvolutionMode>().Length;
    }

    public IAssociatedDataSchema? GetAssociatedData(string name)
    {
        return AssociatedData.TryGetValue(name, out var result) ? result : null;
    }

    public IAssociatedDataSchema GetAssociatedDataOrThrow(string name)
    {
        return AssociatedData.TryGetValue(name, out var result)
            ? result
            : throw new EvitaInvalidUsageException("Associated data `" + name + "` is not known in entity `" + Name +
                                                   "` schema!");
    }

    public IAssociatedDataSchema? GetAssociatedDataByName(string dataName, NamingConvention namingConvention)
    {
        return AssociatedDataNameIndex.TryGetValue(dataName, out var result) ? result[(int) namingConvention] : null;
    }

    public IReferenceSchema? GetReference(string name)
    {
        return References.TryGetValue(name, out var result) ? result : null;
    }

    public IReferenceSchema GetReferenceOrThrowException(string referenceName)
    {
        return References.TryGetValue(referenceName, out var result)
            ? result
            : throw new EvitaInvalidUsageException("Reference `" + referenceName + "` is not known in entity `" + Name +
                                                   "` schema!");
    }

    public IReferenceSchema? GetReferenceByName(string dataName, NamingConvention namingConvention)
    {
        return ReferenceNameIndex.TryGetValue(dataName, out var result) ? result[(int) namingConvention] : null;
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

    internal static EntitySchema InternalBuild(string name)
    {
        return new EntitySchema(
            1,
            name, NamingConventionHelper.Generate(name),
            null, null, false, false, false,
            2,
            new HashSet<CultureInfo>().ToImmutableHashSet(),
            new HashSet<Currency>().ToImmutableHashSet(),
            new Dictionary<string, IAttributeSchema>().ToImmutableDictionary(),
            new Dictionary<string, IAssociatedDataSchema>().ToImmutableDictionary(),
            new Dictionary<string, IReferenceSchema>().ToImmutableDictionary(),
            new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()).ToImmutableHashSet(),
            new Dictionary<string, SortableAttributeCompoundSchema>().ToImmutableDictionary()
        );
    }

    internal static EntitySchema InternalBuild(
        int version,
        string name,
        string? description,
        string? deprecationNotice,
        bool withGeneratedPrimaryKey,
        bool withHierarchy,
        bool withPrice,
        int indexedPricePlaces,
        ISet<CultureInfo> locales,
        ISet<Currency> currencies,
        IDictionary<string, IAttributeSchema> attributes,
        IDictionary<string, IAssociatedDataSchema> associatedData,
        IDictionary<string, IReferenceSchema> references,
        ISet<EvolutionMode> evolutionMode,
        IDictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds
    )
    {
        return new EntitySchema(
            version, name, NamingConventionHelper.Generate(name),
            description, deprecationNotice,
            withGeneratedPrimaryKey, withHierarchy, withPrice,
            indexedPricePlaces,
            locales.ToImmutableHashSet(),
            currencies.ToImmutableHashSet(),
            attributes.ToImmutableDictionary(),
            associatedData.ToImmutableDictionary(),
            references.ToImmutableDictionary(),
            evolutionMode.ToImmutableHashSet(),
            sortableAttributeCompounds.ToImmutableDictionary()
        );
    }

    internal static EntitySchema InternalBuild(
        int version,
        string name,
        IDictionary<NamingConvention, string> nameVariants,
        string? description,
        string? deprecationNotice,
        bool withGeneratedPrimaryKey,
        bool withHierarchy,
        bool withPrice,
        int indexedPricePlaces,
        ISet<CultureInfo> locales,
        ISet<Currency> currencies,
        IDictionary<string, IAttributeSchema> attributes,
        IDictionary<string, IAssociatedDataSchema> associatedData,
        IDictionary<string, IReferenceSchema> references,
        ISet<EvolutionMode> evolutionMode,
        IDictionary<string, SortableAttributeCompoundSchema> sortableAttributeCompounds
    )
    {
        return new EntitySchema(
            version, name, nameVariants,
            description, deprecationNotice,
            withGeneratedPrimaryKey, withHierarchy, withPrice,
            indexedPricePlaces,
            locales.ToImmutableHashSet(),
            currencies.ToImmutableHashSet(),
            attributes.ToImmutableDictionary(),
            associatedData.ToImmutableDictionary(),
            references.ToImmutableDictionary(),
            evolutionMode.ToImmutableHashSet(),
            sortableAttributeCompounds.ToImmutableDictionary()
        );
    }

    public bool DiffersFrom(IEntitySchema? otherSchema)
    {
        if (this == otherSchema) return false;
        if (otherSchema == null) return true;

        if (Version != otherSchema.Version) return true;
        if (WithGeneratedPrimaryKey != otherSchema.WithGeneratedPrimaryKey) return true;
        if (WithHierarchy != otherSchema.WithHierarchy) return true;
        if (WithPrice != otherSchema.WithPrice) return true;
        if (Name != (otherSchema.Name)) return true;
        if (!Locales.Equals(otherSchema.Locales)) return true;
        if (!Currencies.Equals(otherSchema.Currencies)) return true;

        if (Attributes.Count != otherSchema.Attributes.Count) return true;
        foreach (KeyValuePair<string, IAttributeSchema> entry in Attributes)
        {
            IAttributeSchema? otherAttributeSchema = otherSchema.GetAttribute(entry.Key);
            if (!entry.Value.Equals(otherAttributeSchema))
                return true;
        }

        if (AssociatedData.Count != otherSchema.AssociatedData.Count) return true;
        foreach (KeyValuePair<string, IAssociatedDataSchema> entry in AssociatedData)
        {
            if (!entry.Value.Equals(otherSchema.GetAssociatedData(entry.Key)))
                return true;
        }

        if (References.Count != otherSchema.References.Count) return true;
        foreach (KeyValuePair<string, IReferenceSchema> entry in References)
        {
            if (!entry.Value.Equals(otherSchema.GetReference(entry.Key)))
                return true;
        }

        return !Equals(otherSchema.EvolutionModes, EvolutionModes);
    }

    internal static IDictionary<string, T[]> InternalGenerateNameVariantIndex<T>(
        IEnumerable<T> items,
        Func<T, IDictionary<NamingConvention, string>> nameVariantsFetcher
    )
    {
        var list = items.ToList();
        if (!list.Any())
        {
            return new Dictionary<string, T[]>();
        }

        IDictionary<string, T[]> nameIndex = new Dictionary<string, T[]>();
        foreach (T schema in list)
        {
            InternalAddNameVariantsToIndex(nameIndex, schema, nameVariantsFetcher);
        }

        return nameIndex;
    }

    private static void InternalAddNameVariantsToIndex<T>(
        IDictionary<string, T[]> nameIndex,
        T schema,
        Func<T, IDictionary<NamingConvention, string>> nameVariantsFetcher
    )
    {
        foreach (KeyValuePair<NamingConvention, string> entry in nameVariantsFetcher.Invoke(schema))
        {
            T[]? currentArray = nameIndex.TryGetValue(entry.Value, out var existingArray)
                ? existingArray
                : null;
            nameIndex[entry.Value] = currentArray ?? new T[Enum.GetValues<NamingConvention>().Length];
        }
    }

    public string GetNameVariant(NamingConvention namingConvention) => NameVariants[namingConvention];

    public ISet<EvolutionMode> GetEvolutionMode()
    {
        return EvolutionModes;
    }

    public bool Allows(EvolutionMode evolutionMode)
    {
        return GetEvolutionMode().Contains(evolutionMode);
    }

    public bool SupportsLocale(CultureInfo locale)
    {
        return Locales.Contains(locale);
    }
    
    private static AttributeSchema ToAttributeSchema(IAttributeSchema attributeSchemaContract) {
        return attributeSchemaContract as AttributeSchema ?? AttributeSchema.InternalBuild(
            attributeSchemaContract.Name,
            attributeSchemaContract.NameVariants,
            attributeSchemaContract.Description,
            attributeSchemaContract.DeprecationNotice,
            attributeSchemaContract.Unique,
            attributeSchemaContract.Filterable,
            attributeSchemaContract.Sortable,
            attributeSchemaContract.Localized,
            attributeSchemaContract.Nullable,
            attributeSchemaContract.Type,
            attributeSchemaContract.DefaultValue,
            attributeSchemaContract.IndexedDecimalPlaces
        );
    }

    public static SortableAttributeCompoundSchema ToSortableAttributeCompoundSchema(
        ISortableAttributeCompoundSchema sortableAttributeCompoundSchemaContract)
    {
        return sortableAttributeCompoundSchemaContract as SortableAttributeCompoundSchema ?? SortableAttributeCompoundSchema.InternalBuild(
            sortableAttributeCompoundSchemaContract.Name,
            sortableAttributeCompoundSchemaContract.NameVariants,
            sortableAttributeCompoundSchemaContract.Description,
            sortableAttributeCompoundSchemaContract.DeprecationNotice,
            sortableAttributeCompoundSchemaContract.AttributeElements
        );
    }
    
    private static AssociatedDataSchema ToAssociatedDataSchema(IAssociatedDataSchema associatedDataSchemaContract) {
        return associatedDataSchemaContract as AssociatedDataSchema ?? AssociatedDataSchema.InternalBuild(
            associatedDataSchemaContract.Name,
            associatedDataSchemaContract.NameVariants,
            associatedDataSchemaContract.Description,
            associatedDataSchemaContract.DeprecationNotice,
            associatedDataSchemaContract.Localized,
            associatedDataSchemaContract.Nullable,
            associatedDataSchemaContract.Type
        );
    }
    
    private static ReferenceSchema ToReferenceSchema(IReferenceSchema referenceSchemaContract) {
        return referenceSchemaContract as ReferenceSchema ?? ReferenceSchema.InternalBuild(
            referenceSchemaContract.Name,
            referenceSchemaContract.NameVariants,
            referenceSchemaContract.Description,
            referenceSchemaContract.DeprecationNotice,
            referenceSchemaContract.ReferencedEntityType,
            referenceSchemaContract.GetEntityTypeNameVariants(_ => null),
            referenceSchemaContract.ReferencedEntityTypeManaged,
            referenceSchemaContract.Cardinality,
            referenceSchemaContract.ReferencedGroupType,
            referenceSchemaContract.GetGroupTypeNameVariants(_ => null),
            referenceSchemaContract.ReferencedGroupTypeManaged,
            referenceSchemaContract.Indexed,
            referenceSchemaContract.Faceted,
            referenceSchemaContract.GetAttributes(),
            referenceSchemaContract.GetSortableAttributeCompounds()
        );
    }

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
    
    private record AttributeToCompound(AttributeElement Attribute, SortableAttributeCompoundSchema CompoundSchema);

}