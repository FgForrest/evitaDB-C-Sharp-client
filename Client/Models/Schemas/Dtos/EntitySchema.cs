using System.Collections.Immutable;
using System.Globalization;
using Client.DataTypes;
using Client.Exceptions;
using Client.Utils;

namespace Client.Models.Schemas.Dtos;

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
    public IEnumerable<AttributeSchema> NonNullableAttributes { get; }
    public IEnumerable<AssociatedDataSchema> NonNullableAssociatedData { get; }
    public IDictionary<string, AttributeSchema> Attributes { get; }
    private IDictionary<string, AttributeSchema[]> AttributeNameIndex { get; }
    public IDictionary<string, AssociatedDataSchema> AssociatedData { get; }
    private IDictionary<string, AssociatedDataSchema[]> AssociatedDataNameIndex { get; }
    public IDictionary<string, ReferenceSchema> References { get; }
    private IDictionary<string, ReferenceSchema[]> ReferenceNameIndex { get; }

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
        IDictionary<string, AttributeSchema> attributes,
        IDictionary<string, AssociatedDataSchema> associatedData,
        IDictionary<string, ReferenceSchema> references,
        ISet<EvolutionMode> evolutionMode
    )
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
            InternalGenerateNameVariantIndex(Attributes.Values, x=>x.NameVariants);
        AssociatedData = associatedData.ToDictionary(x => x.Key, x => x.Value);
        AssociatedDataNameIndex =
            InternalGenerateNameVariantIndex(AssociatedData.Values, x=>x.NameVariants);
        References = references.ToDictionary(x => x.Key, x => x.Value);
        ReferenceNameIndex =
            InternalGenerateNameVariantIndex(References.Values, x=>x.NameVariants);
        EvolutionModes = evolutionMode;
        NonNullableAttributes = this.Attributes
            .Values
            .Where(it => !it.Nullable)
            .ToList();
        NonNullableAssociatedData = AssociatedData
            .Values
            .Where(it => !it.Nullable)
            .ToList();
    }

    public bool IsBlank()
    {
        return Version == 1 && !WithGeneratedPrimaryKey && !WithHierarchy && !WithPrice && IndexedPricePlaces == 2 &&
               !Locales.Any() && !Currencies.Any() && !Attributes.Any() && !AssociatedData.Any() && !References.Any()
               && EvolutionModes.Count == Enum.GetValues<EvolutionMode>().Length;
    }

    public AssociatedDataSchema? GetAssociatedData(string name)
    {
        return AssociatedData.TryGetValue(name, out var result) ? result : null;
    }

    public AssociatedDataSchema GetAssociatedDataOrThrow(string name)
    {
        return AssociatedData.TryGetValue(name, out var result)
            ? result
            : throw new EvitaInvalidUsageException("Associated data `" + name + "` is not known in entity `" + Name +
                                                   "` schema!");
    }

    public AssociatedDataSchema? GetAssociatedDataByName(string dataName, NamingConvention namingConvention)
    {
        return AssociatedDataNameIndex.TryGetValue(dataName, out var result) ? result[(int) namingConvention] : null;
    }

    public ReferenceSchema? GetReference(string name)
    {
        return References.TryGetValue(name, out var result) ? result : null;
    }

    public ReferenceSchema GetReferenceOrThrow(string name)
    {
        return References.TryGetValue(name, out var result)
            ? result
            : throw new EvitaInvalidUsageException("Reference `" + name + "` is not known in entity `" + Name +
                                                   "` schema!");
    }

    public ReferenceSchema? GetReferenceByName(string dataName, NamingConvention namingConvention)
    {
        return ReferenceNameIndex.TryGetValue(dataName, out var result) ? result[(int) namingConvention] : null;
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
    
    public static EntitySchema InternalBuild(string name) {
		return new EntitySchema(
			1,
			name, NamingConventionHelper.Generate(name),
			null, null, false, false, false,
			2,
			new HashSet<CultureInfo>().ToImmutableHashSet(),
			new HashSet<Currency>().ToImmutableHashSet(),
			new Dictionary<string, AttributeSchema>().ToImmutableDictionary(),
			new Dictionary<string, AssociatedDataSchema>().ToImmutableDictionary(),
			new Dictionary<string, ReferenceSchema>().ToImmutableDictionary(),
			new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()).ToImmutableHashSet()
		);
	}

	public static EntitySchema InternalBuild(
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
		IDictionary<string, AttributeSchema> attributes,
		IDictionary<string, AssociatedDataSchema> associatedData,
		IDictionary<string, ReferenceSchema> references,
		ISet<EvolutionMode> evolutionMode
	) {
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
			evolutionMode.ToImmutableHashSet()
		);
	}

	public static EntitySchema InternalBuild(
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
		IDictionary<string, AttributeSchema> attributes,
		IDictionary<string, AssociatedDataSchema> associatedData,
		IDictionary<string, ReferenceSchema> references,
		ISet<EvolutionMode> evolutionMode
	) {
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
			evolutionMode.ToImmutableHashSet()
		);
	}

    public bool DiffersFrom(EntitySchema? otherSchema)
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
        foreach (KeyValuePair<string, AttributeSchema> entry in Attributes)
        {
            AttributeSchema? otherAttributeSchema = otherSchema.GetAttribute(entry.Key);
            if (!entry.Value.Equals(otherAttributeSchema))
                return true;
        }

        if (AssociatedData.Count != otherSchema.AssociatedData.Count) return true;
        foreach (KeyValuePair<string, AssociatedDataSchema> entry in AssociatedData)
        {
            if (!entry.Value.Equals(otherSchema.GetAssociatedData(entry.Key)))
                return true;
        }

        if (References.Count != otherSchema.References.Count) return true;
        foreach (KeyValuePair<string, ReferenceSchema> entry in References)
        {
            if (!entry.Value.Equals(otherSchema.GetReference(entry.Key)))
                return true;
        }

        return !Equals(otherSchema.EvolutionModes, EvolutionModes);
    }

    public static IDictionary<string, T[]> InternalGenerateNameVariantIndex<T>(
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

    public bool Allows(EvolutionMode evolutionMode) {
	    return GetEvolutionMode().Contains(evolutionMode);
    }

    public bool SupportsLocale(CultureInfo locale) {
	    return Locales.Contains(locale);
    }
}