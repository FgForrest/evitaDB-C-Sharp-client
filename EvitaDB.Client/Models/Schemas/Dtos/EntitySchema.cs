﻿using System.Collections.Immutable;
using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class EntitySchema : IEntitySchema
{
    public int Version { get; }
    public string Name { get; }
    public IDictionary<NamingConvention, string?> NameVariants { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public bool WithGeneratedPrimaryKey() => withGeneratedPrimaryKey;
    public bool WithHierarchy() => withHierarchy;
    public bool WithPrice() => withPrice;
    public int IndexedPricePlaces { get; }
    public ISet<CultureInfo> Locales { get; }
    public ISet<Currency> Currencies { get; }
    public ISet<EvolutionMode> EvolutionModes { get; }
    public IEnumerable<IEntityAttributeSchema> NonNullableAttributes { get; }
    public IEnumerable<IAssociatedDataSchema> NonNullableAssociatedData { get; }

    public IDictionary<string, IEntityAttributeSchema> Attributes { get; } =
        new Dictionary<string, IEntityAttributeSchema>();
    private IDictionary<string, IEntityAttributeSchema[]> AttributeNameIndex { get; }
    private IDictionary<string, SortableAttributeCompoundSchema> SortableAttributeCompounds { get; }
    private IDictionary<string, SortableAttributeCompoundSchema[]> SortableAttributeCompoundNameIndex { get; }
    private IDictionary<string, List<SortableAttributeCompoundSchema>> AttributeToSortableAttributeCompoundIndex { get; }

    public IDictionary<string, IAssociatedDataSchema> AssociatedData { get; } =
        new Dictionary<string, IAssociatedDataSchema>();
    private IDictionary<string, IAssociatedDataSchema[]> AssociatedDataNameIndex { get; }
    public IDictionary<string, IReferenceSchema> References { get; }
    private IDictionary<string, IReferenceSchema[]> ReferenceNameIndex { get; }
    public IList<IEntityAttributeSchema> OrderedAttributes { get; } = new List<IEntityAttributeSchema>();
    public IList<IAssociatedDataSchema> OrderedAssociatedData { get; } = new List<IAssociatedDataSchema>();

    private bool withGeneratedPrimaryKey;
    private bool withHierarchy;
    private bool withPrice;
    
    private EntitySchema(
        int version,
        string name,
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        bool withGeneratedPrimaryKey,
        bool withHierarchy,
        bool withPrice,
        int indexedPricePlaces,
        ISet<CultureInfo> locales,
        ISet<Currency> currencies,
        IDictionary<string, IEntityAttributeSchema> attributes,
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
        this.withGeneratedPrimaryKey = withGeneratedPrimaryKey;
        this.withHierarchy = withHierarchy;
        this.withPrice = withPrice;
        IndexedPricePlaces = indexedPricePlaces;
        Locales = locales.ToImmutableSortedSet(Comparer<CultureInfo>.Create((x, y) => string.Compare(x.TwoLetterISOLanguageName, y.TwoLetterISOLanguageName, StringComparison.Ordinal)));
        Currencies = currencies.ToImmutableSortedSet(Comparer<Currency>.Create((x, y) => string.Compare(x.CurrencyCode, y.CurrencyCode, StringComparison.Ordinal)));
        
        foreach (var (key, value) in attributes)
        {
            Attributes.Add(key, value);
            OrderedAttributes.Add(value);
        }
        AttributeNameIndex =
            InternalGenerateNameVariantIndex(Attributes.Values, x => x.NameVariants);
        Attributes = Attributes.ToImmutableDictionary();
        
        foreach (var (key, value) in associatedData)
        {
            AssociatedData.Add(key, value);
            OrderedAssociatedData.Add(value);
        }
        AssociatedDataNameIndex =
            InternalGenerateNameVariantIndex(AssociatedData.Values, x => x.NameVariants);
        AssociatedData = AssociatedData.ToImmutableDictionary();
        
        References = references.ToImmutableDictionary(x => x.Key, x => x.Value);
        ReferenceNameIndex =
            InternalGenerateNameVariantIndex(References.Values, x => x.NameVariants);
        EvolutionModes = evolutionMode;
        NonNullableAttributes = Attributes
            .Values
            .Where(it => !it.Nullable())
            .ToList();
        NonNullableAssociatedData = AssociatedData
            .Values
            .Where(it => !it.Nullable())
            .ToList();
        SortableAttributeCompounds = sortableAttributeCompounds.ToImmutableDictionary(
            x => x.Key,
            x => ToSortableAttributeCompoundSchema(x.Value)
        );

        SortableAttributeCompoundNameIndex = InternalGenerateNameVariantIndex(
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
        return Version == 1 && !WithGeneratedPrimaryKey() && !WithHierarchy() && !WithPrice() && IndexedPricePlaces == 2 &&
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
    
    public IDictionary<string, IEntityAttributeSchema> GetAttributes()
    {
        return Attributes;
    }

    public IEntityAttributeSchema? GetAttribute(string name)
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

    public IEntityAttributeSchema? GetAttributeByName(string dataName, NamingConvention namingConvention)
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
            new Dictionary<string, IEntityAttributeSchema>().ToImmutableDictionary(),
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
        IDictionary<string, IEntityAttributeSchema> attributes,
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
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        bool withGeneratedPrimaryKey,
        bool withHierarchy,
        bool withPrice,
        int indexedPricePlaces,
        ISet<CultureInfo> locales,
        ISet<Currency> currencies,
        IDictionary<string, IEntityAttributeSchema> attributes,
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
            attributes,
            associatedData,
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
        foreach (KeyValuePair<string, IEntityAttributeSchema> entry in Attributes)
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
        Func<T, IDictionary<NamingConvention, string?>> nameVariantsFetcher
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
        Func<T, IDictionary<NamingConvention, string?>> nameVariantsFetcher
    )
    {
        foreach (KeyValuePair<NamingConvention, string?> entry in nameVariantsFetcher.Invoke(schema))
        {
            T[]? currentArray = nameIndex.TryGetValue(entry.Value ?? throw new InvalidOperationException(), out var existingArray)
                ? existingArray
                : null;
            nameIndex[entry.Value] = currentArray ?? new T[Enum.GetValues<NamingConvention>().Length];
        }
    }

    public string? GetNameVariant(NamingConvention namingConvention) => NameVariants[namingConvention];

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
            attributeSchemaContract.UniquenessType,
            attributeSchemaContract.Filterable(),
            attributeSchemaContract.Sortable(),
            attributeSchemaContract.Localized(),
            attributeSchemaContract.Nullable(),
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
            associatedDataSchemaContract.Localized(),
            associatedDataSchemaContract.Nullable(),
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
            referenceSchemaContract.GetEntityTypeNameVariants(_ => null!),
            referenceSchemaContract.ReferencedEntityTypeManaged,
            referenceSchemaContract.Cardinality,
            referenceSchemaContract.ReferencedGroupType,
            referenceSchemaContract.GetGroupTypeNameVariants(_ => null!),
            referenceSchemaContract.ReferencedGroupTypeManaged,
            referenceSchemaContract.IsIndexed,
            referenceSchemaContract.IsFaceted,
            referenceSchemaContract.GetAttributes(),
            referenceSchemaContract.GetSortableAttributeCompounds()
        );
    }
    
    internal static AttributeSchema ToReferenceAttributeSchema(IAttributeSchema attributeSchemaContract) {
        return attributeSchemaContract as AttributeSchema ?? AttributeSchema.InternalBuild(
            attributeSchemaContract.Name,
            attributeSchemaContract.NameVariants,
            attributeSchemaContract.Description,
            attributeSchemaContract.DeprecationNotice,
            attributeSchemaContract.UniquenessType,
            attributeSchemaContract.Filterable(),
            attributeSchemaContract.Sortable(),
            attributeSchemaContract.Localized(),
            attributeSchemaContract.Nullable(),
            attributeSchemaContract.Type,
            attributeSchemaContract.DefaultValue,
            attributeSchemaContract.IndexedDecimalPlaces
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
