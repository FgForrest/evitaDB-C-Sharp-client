using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;
using EvitaDB.Client.Models.Schemas.Mutations.References;
using EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;
using EvitaDB.Client.Utils;
using static EvitaDB.Client.Models.Schemas.Builders.SchemaBuilderHelper;

namespace EvitaDB.Client.Models.Schemas.Builders;

public class InternalEntitySchemaBuilder : IEntitySchemaBuilder
{
    public IEntitySchema BaseSchema { get; }
    public List<IEntitySchemaMutation> Mutations { get; } = new();
    private Func<ICatalogSchema> CatalogSchemaAccessor { get; set; }
    private bool UpdatedSchemaDirty { get; set; }
    private IEntitySchema? UpdatedSchema { get; set; }

    private readonly IEntitySchema _instance;

    public int Version => _instance.Version;
    public string Name => _instance.Name;
    public string? Description => _instance.Description;
    public string? DeprecationNotice => _instance.DeprecationNotice;
    public IDictionary<NamingConvention, string?> NameVariants => _instance.NameVariants;
    bool IEntitySchema.WithGeneratedPrimaryKey => _instance.WithGeneratedPrimaryKey;
    bool IEntitySchema.WithHierarchy => _instance.WithHierarchy;
    bool IEntitySchema.WithPrice => _instance.WithPrice;
    public int IndexedPricePlaces => _instance.IndexedPricePlaces;
    public ISet<CultureInfo> Locales => _instance.Locales;
    public ISet<Currency> Currencies => _instance.Currencies;
    public ISet<EvolutionMode> EvolutionModes => _instance.EvolutionModes;
    public IEnumerable<IEntityAttributeSchema> NonNullableAttributes => _instance.NonNullableAttributes;
    public IEnumerable<IAssociatedDataSchema> NonNullableAssociatedData => _instance.NonNullableAssociatedData;
    public IDictionary<string, IEntityAttributeSchema> Attributes => _instance.Attributes;
    public IDictionary<string, IAssociatedDataSchema> AssociatedData => _instance.AssociatedData;
    public IDictionary<string, IReferenceSchema> References => _instance.References;

    public InternalEntitySchemaBuilder(
        ICatalogSchema catalogSchema,
        IEntitySchema baseSchema,
        ICollection<IEntitySchemaMutation> schemaMutations)
    {
        CatalogSchemaAccessor = () => catalogSchema;
        BaseSchema = baseSchema;
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations, schemaMutations.ToArray()
        );
        _instance ??= ToInstance();
    }

    public InternalEntitySchemaBuilder(
        ICatalogSchema catalogSchema,
        IEntitySchema baseSchema
    ) : this(catalogSchema, baseSchema, new List<IEntitySchemaMutation>())
    {
    }


    public InternalEntitySchemaBuilder CooperatingWith(Func<ICatalogSchema> catalogSupplier)
    {
        CatalogSchemaAccessor = catalogSupplier;
        return this;
    }

    IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.WithGeneratedPrimaryKey()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithGeneratedPrimaryKeyMutation(true)
        );
        return this;
    }

    public IEntitySchemaBuilder WithoutGeneratedPrimaryKey()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithGeneratedPrimaryKeyMutation(false)
        );
        return this;
    }

    IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.WithHierarchy()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithHierarchyMutation(true)
        );
        return this;
    }

    public IEntitySchemaBuilder WithoutHierarchy()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithHierarchyMutation(false)
        );
        return this;
    }

    public IEntitySchemaBuilder WithAttribute<T>(string attributeName)
    {
        return WithAttribute<T>(attributeName, null);
    }

    public IEntitySchemaBuilder WithoutAttribute(string attributeName)
    {
        CheckSortableAttributeCompoundsWithoutAttribute(
            attributeName, GetSortableAttributeCompounds().Values
        );
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new RemoveAttributeSchemaMutation(attributeName)
        );
        return this;
    }

    public IEntitySchemaBuilder WithSortableAttributeCompound(
        string name,
        params AttributeElement[] attributeElements
    )
    {
        return WithSortableAttributeCompound(
            name, attributeElements, null
        );
    }

    public IEntitySchemaBuilder WithSortableAttributeCompound(
        string name,
        AttributeElement[] attributeElements,
        Action<SortableAttributeCompoundSchemaBuilder>? whichIs
    )
    {
        ISortableAttributeCompoundSchema? existingCompound = GetSortableAttributeCompound(name);
        ICatalogSchema catalogSchema = CatalogSchemaAccessor.Invoke();
        SortableAttributeCompoundSchemaBuilder builder = new SortableAttributeCompoundSchemaBuilder(
            catalogSchema,
            this,
            null,
            existingCompound,
            name,
            attributeElements.ToList(),
            new List<IEntitySchemaMutation>(),
            true
        );
        SortableAttributeCompoundSchemaBuilder schemaBuilder;
        if (existingCompound is not null)
        {
            Assert.IsTrue(
                existingCompound.AttributeElements.SequenceEqual(attributeElements.ToList()),
                () => new AttributeAlreadyPresentInEntitySchemaException(
                    existingCompound, builder.ToInstance(), null, name
                )
            );
            schemaBuilder = builder;
        }
        else
        {
            schemaBuilder = builder;
        }

        whichIs?.Invoke(schemaBuilder);
        ISortableAttributeCompoundSchema compoundSchema = schemaBuilder.ToInstance();
        Assert.IsTrue(
            compoundSchema.AttributeElements.Count > 1,
            ()=> new SortableAttributeCompoundSchemaException(
                "Sortable attribute compound requires more than one attribute element!",
                compoundSchema
            )
            );
        Assert.IsTrue(
            compoundSchema.AttributeElements.Count ==
            compoundSchema.AttributeElements
                .Select(x=>x.AttributeName)
                .Distinct()
                .Count(),
            ()=> new SortableAttributeCompoundSchemaException(
                "Attribute names of elements in sortable attribute compound must be unique!",
                compoundSchema
            )
            );
        CheckSortableTraits(name, compoundSchema, GetAttributes());

        // check the names in all naming conventions are unique in the catalog schema
        CheckNamesAreUniqueInAllNamingConventions(
            GetAttributes().Values,
            GetSortableAttributeCompounds().Values,
            compoundSchema
        );

        UpdatedSchemaDirty = AddMutations(
            catalogSchema, this, Mutations,
            new CreateSortableAttributeCompoundSchemaMutation(
                compoundSchema.Name,
                compoundSchema.Description,
                compoundSchema.DeprecationNotice,
                attributeElements
            )
        );
        return this;
    }

    public IEntitySchemaBuilder WithoutSortableAttributeCompound(string name)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new RemoveSortableAttributeCompoundSchemaMutation(name)
        );
        return this;
    }

    IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.WithPrice()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithPriceMutation(true, 2)
        );
        return this;
    }

    IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.WithPrice(int indexedDecimalPlaces)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithPriceMutation(true, indexedDecimalPlaces)
        );
        return this;
    }

    public IEntitySchemaBuilder VerifySchemaStrictly()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new DisallowEvolutionModeInEntitySchemaMutation(Enum.GetValues<EvolutionMode>())
        );
        return this;
    }

    public IEntitySchemaBuilder VerifySchemaButAllow(params EvolutionMode[] evolutionMode)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new DisallowEvolutionModeInEntitySchemaMutation(Enum.GetValues<EvolutionMode>()),
            new AllowEvolutionModeInEntitySchemaMutation(evolutionMode)
        );
        return this;
    }

    public IEntitySchemaBuilder VerifySchemaButCreateOnTheFly()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new AllowEvolutionModeInEntitySchemaMutation(Enum.GetValues<EvolutionMode>())
        );
        return this;
    }

    public IEntitySchemaBuilder WithDescription(string? description)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new ModifyEntitySchemaDescriptionMutation(description)
        );
        return this;
    }

    public IEntitySchemaBuilder Deprecated(string deprecationNotice)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new ModifyEntitySchemaDeprecationNoticeMutation(deprecationNotice)
        );
        return this;
    }

    public IEntitySchemaBuilder NotDeprecatedAnymore()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new ModifyEntitySchemaDeprecationNoticeMutation(null)
        );
        return this;
    }

    public IEntitySchemaBuilder WithGeneratedPrimaryKey()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithGeneratedPrimaryKeyMutation(true)
        );
        return this;
    }

    public IEntitySchemaBuilder WithHierarchy()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithHierarchyMutation(true)
        );
        return this;
    }

    public IEntitySchemaBuilder WithPrice()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithPriceMutation(true, 2)
        );
        return this;
    }

    public IEntitySchemaBuilder WithPrice(int indexedDecimalPlaces)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithPriceMutation(true, indexedDecimalPlaces)
        );
        return this;
    }

    public IEntitySchemaBuilder WithPriceInCurrency(params Currency[] currency)
    {
        return WithPriceInCurrency(2, currency);
    }

    public IEntitySchemaBuilder WithPriceInCurrency(int indexedPricePlaces, params Currency[] currency)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithPriceMutation(true, indexedPricePlaces),
            new AllowCurrencyInEntitySchemaMutation(currency)
        );
        return this;
    }

    public IEntitySchemaBuilder WithoutPrice()
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new SetEntitySchemaWithPriceMutation(false, 0)
        );
        return this;
    }

    public IEntitySchemaBuilder WithoutPriceInCurrency(Currency currency)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new DisallowCurrencyInEntitySchemaMutation(currency)
        );
        return this;
    }

    public IEntitySchemaBuilder WithLocale(params CultureInfo[] locale)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new AllowLocaleInEntitySchemaMutation(locale)
        );
        return this;
    }

    public IEntitySchemaBuilder WithoutLocale(CultureInfo locale)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new DisallowLocaleInEntitySchemaMutation(locale)
        );
        return this;
    }

    public IEntitySchemaBuilder WithGlobalAttribute(string attributeName)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new UseGlobalAttributeSchemaMutation(attributeName)
        );
        return this;
    }

    public IEntitySchemaBuilder WithAttribute<T>(
        string attributeName,
        Action<IEntityAttributeSchemaBuilder>? whichIs)
    {
        ICatalogSchema catalogSchema = CatalogSchemaAccessor.Invoke();
        IGlobalAttributeSchema? existingAttributeSchema = catalogSchema.GetAttribute(attributeName);
        if (existingAttributeSchema is not null)
        {
            throw new AttributeAlreadyPresentInCatalogSchemaException(
                catalogSchema.Name, existingAttributeSchema
            );
        }

        IEntityAttributeSchema? existingAttribute = BaseSchema.GetAttribute(attributeName);

        EntityAttributeSchemaBuilder attributeSchemaBuilder;

        if (existingAttribute is not null)
        {
            attributeSchemaBuilder = new EntityAttributeSchemaBuilder(BaseSchema, existingAttribute);
            Assert.IsTrue(
                typeof(T) == existingAttribute.Type,
                () => new AttributeAlreadyPresentInEntitySchemaException(existingAttribute,
                    attributeSchemaBuilder.ToInstance(), null, attributeName)
            );
        }
        else
        {
            attributeSchemaBuilder =
                new EntityAttributeSchemaBuilder(BaseSchema, attributeName, typeof(T));
        }

        whichIs?.Invoke(attributeSchemaBuilder);
        IEntityAttributeSchema attributeSchema = attributeSchemaBuilder.ToInstance();
        CheckSortableTraits(attributeName, attributeSchema);

        // check the names in all naming conventions are unique in the catalog schema
        CheckNamesAreUniqueInAllNamingConventions(Attributes.Values, GetSortableAttributeCompounds().Values,
            attributeSchema);

        if (existingAttribute is not null && existingAttribute.Equals(attributeSchema) || true)
        {
            UpdatedSchemaDirty = AddMutations(
                catalogSchema, BaseSchema, Mutations,
                attributeSchemaBuilder.ToMutation().ToArray()
            );
        }

        return this;
    }

    public IEntitySchemaBuilder WithAssociatedData<T>(string dataName)
    {
        return WithAssociatedData<T>(dataName, null);
    }

    public IEntitySchemaBuilder WithAssociatedData<T>(
        string dataName,
        Action<IAssociatedDataSchemaEditor>? whichIs
    )
    {
        IAssociatedDataSchema? existingAssociatedData = BaseSchema.GetAssociatedData(dataName);
        ICatalogSchema catalogSchema = CatalogSchemaAccessor.Invoke();
        AssociatedDataSchemaBuilder associatedDataSchemaBuilder;
        if (existingAssociatedData is not null)
        {
            Type typeToCompare = EvitaDataTypes.IsSupportedTypeOrItsArray(typeof(T))
                ? typeof(T)
                : typeof(ComplexDataObject);
            Assert.IsTrue(
                typeToCompare == existingAssociatedData.Type,
                () => new InvalidSchemaMutationException(
                    "Associated data " + dataName + " has already assigned type " + existingAssociatedData.Type +
                    ", cannot change this type to: " + typeof(T) + "!"
                )
            );
            associatedDataSchemaBuilder =
                new AssociatedDataSchemaBuilder(catalogSchema, BaseSchema, existingAssociatedData);
        }
        else
        {
            associatedDataSchemaBuilder =
                new AssociatedDataSchemaBuilder(catalogSchema, BaseSchema, dataName, typeof(T));
        }

        whichIs?.Invoke(associatedDataSchemaBuilder);
        IAssociatedDataSchema associatedDataSchema = associatedDataSchemaBuilder.ToInstance();

        if (existingAssociatedData is not null && !existingAssociatedData.Equals(associatedDataSchema) || true)
        {
            ClassifierUtils.ValidateClassifierFormat(ClassifierType.AssociatedData, dataName);
            // check the names in all naming conventions are unique in the entity schema
            BaseSchema.AssociatedData
                .Values
                .Where(it => !Equals(it.Name, associatedDataSchema.Name))
                .SelectMany(it => it.NameVariants
                    .Where(nameVariant =>
                        nameVariant.Value!.Equals(associatedDataSchema.GetNameVariant(nameVariant.Key)))
                    .Select(nameVariant =>
                        new AssociatedDataNamingConventionConflict(it, nameVariant.Key, nameVariant.Value!))
                ).ToList()
                .ForEach(conflict => throw new AssociatedDataAlreadyPresentInEntitySchemaException(
                    conflict.ConflictingSchema, associatedDataSchema,
                    conflict.Convention, conflict.ConflictingName
                ));
            UpdatedSchemaDirty = AddMutations(
                catalogSchema, BaseSchema, Mutations,
                associatedDataSchemaBuilder.ToMutation().ToArray()
            );
        }

        return this;
    }

    public IEntitySchemaBuilder WithoutAssociatedData(string dataName)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new RemoveAssociatedDataSchemaMutation(dataName)
        );
        return this;
    }

    public IEntitySchemaBuilder WithReferenceTo(string name, string externalEntityType, Cardinality cardinality)
    {
        return WithReferenceTo(name, externalEntityType, cardinality, null);
    }

    public IEntitySchemaBuilder WithReferenceTo(string name, string externalEntityType, Cardinality cardinality,
        Action<IReferenceSchemaBuilder>? whichIs)
    {
        IEntitySchema currentSchema = ToInstance();
        IReferenceSchema? existingReference = currentSchema.GetReference(name);
        ReferenceSchemaBuilder referenceBuilder = new ReferenceSchemaBuilder(
            CatalogSchemaAccessor.Invoke(),
            BaseSchema,
            existingReference,
            name,
            externalEntityType,
            false,
            cardinality,
            Mutations,
            BaseSchema.GetReference(name) is null
        );
        whichIs?.Invoke(referenceBuilder);
        RedefineReferenceType(
            referenceBuilder,
            existingReference
        );
        return this;
    }

    public IEntitySchemaBuilder WithReferenceToEntity(string name, string entityType, Cardinality cardinality)
    {
        return WithReferenceToEntity(name, entityType, cardinality, null);
    }

    public IEntitySchemaBuilder WithReferenceToEntity(string name, string entityType, Cardinality cardinality,
        Action<IReferenceSchemaBuilder>? whichIs)
    {
        IEntitySchema currentSchema = ToInstance();
        IReferenceSchema? existingReference = currentSchema.GetReference(name);
        ReferenceSchemaBuilder referenceSchemaBuilder = new ReferenceSchemaBuilder(
            CatalogSchemaAccessor.Invoke(),
            BaseSchema,
            existingReference,
            name,
            entityType,
            true,
            cardinality,
            Mutations,
            BaseSchema.GetReference(name) is null
        );
        whichIs?.Invoke(referenceSchemaBuilder);
        RedefineReferenceType(
            referenceSchemaBuilder,
            existingReference
        );
        return this;
    }

    public IEntitySchemaBuilder WithoutReferenceTo(string name)
    {
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
            new RemoveReferenceSchemaMutation(name)
        );
        return this;
    }

    public ModifyEntitySchemaMutation? ToMutation()
    {
        return Mutations.Any() ? new ModifyEntitySchemaMutation(BaseSchema.Name, Mutations.ToArray()) : null;
    }

    public IEntitySchema ToInstance()
    {
        if (UpdatedSchema == null || UpdatedSchemaDirty)
        {
            IEntitySchema? currentSchema = BaseSchema;
            foreach (IEntitySchemaMutation mutation in Mutations)
            {
                currentSchema = mutation.Mutate(CatalogSchemaAccessor.Invoke(), currentSchema);
                if (currentSchema == null)
                {
                    throw new EvitaInternalError("Catalog schema unexpectedly removed from inside!");
                }
            }

            UpdatedSchema = currentSchema;
            UpdatedSchemaDirty = false;
        }

        return UpdatedSchema;
    }


    void RedefineReferenceType(
        ReferenceSchemaBuilder referenceSchemaBuilder,
        IReferenceSchema? existingReference
    )
    {
        IReferenceSchema newReference = referenceSchemaBuilder.ToInstance();
        if (!Equals(existingReference, newReference))
        {
            // remove all existing mutations for the reference schema (it needs to be replaced)
            Mutations.RemoveAll(it =>
                it is IReferenceSchemaMutation referenceSchemaMutation &&
                referenceSchemaMutation.Name.Equals(newReference.Name));
            // check the names in all naming conventions are unique in the entity schema
            ToInstance()
                .References
                .Values
                .Where(it => !Equals(it.Name, referenceSchemaBuilder.Name))
                .SelectMany(
                    it => it.NameVariants
                        .Where(nameVariant =>
                            nameVariant.Value!.Equals(referenceSchemaBuilder.GetNameVariant(nameVariant.Key)))
                        .Select(nameVariant =>
                            new ReferenceNamingConventionConflict(it, nameVariant.Key, nameVariant.Value!))
                )
                .ToList()
                .ForEach(conflict => throw new ReferenceAlreadyPresentInEntitySchemaException(
                    conflict.ConflictingSchema, referenceSchemaBuilder,
                    conflict.Convention, conflict.ConflictingName
                ));
            UpdatedSchemaDirty = AddMutations(
                CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
                referenceSchemaBuilder.ToMutation().ToArray()
            );
        }
    }


    /**
     * DTO for passing the identified conflict in attribute names for certain naming convention.
     */
    public record AttributeNamingConventionConflict(IAttributeSchema? ConflictingAttributeSchema,
        ISortableAttributeCompoundSchema? ConflictingCompoundSchema,
        NamingConvention Convention, string ConflictingName);

    /**
     * DTO for passing the identified conflict in associated data names for certain naming convention.
     */
    private record AssociatedDataNamingConventionConflict(IAssociatedDataSchema ConflictingSchema,
        NamingConvention Convention,
        string ConflictingName
    );

    /**
     * DTO for passing the identified conflict in reference names for certain naming convention.
     */
    private record ReferenceNamingConventionConflict(IReferenceSchema ConflictingSchema, NamingConvention Convention,
        string ConflictingName
    );

    public string? GetNameVariant(NamingConvention namingConvention)
    {
        return _instance.GetNameVariant(namingConvention);
    }


    public IDictionary<string, IEntityAttributeSchema> GetAttributes()
    {
        return _instance.GetAttributes();
    }

    public bool DiffersFrom(IEntitySchema? otherSchema)
    {
        return _instance.DiffersFrom(otherSchema);
    }

    public ISet<EvolutionMode> GetEvolutionMode()
    {
        return _instance.GetEvolutionMode();
    }

    public bool Allows(EvolutionMode evolutionMode)
    {
        return _instance.Allows(evolutionMode);
    }

    public bool SupportsLocale(CultureInfo locale)
    {
        return _instance.SupportsLocale(locale);
    }

    public IAttributeSchema GetAttributeOrThrow(string name)
    {
        return _instance.GetAttributeOrThrow(name);
    }

    public bool IsBlank()
    {
        return _instance.IsBlank();
    }

    public IAssociatedDataSchema? GetAssociatedData(string name)
    {
        return _instance.GetAssociatedData(name);
    }

    public IAssociatedDataSchema GetAssociatedDataOrThrow(string name)
    {
        return _instance.GetAssociatedDataOrThrow(name);
    }

    public IAssociatedDataSchema? GetAssociatedDataByName(string dataName, NamingConvention namingConvention)
    {
        return _instance.GetAssociatedDataByName(dataName, namingConvention);
    }

    public IReferenceSchema? GetReference(string name)
    {
        return _instance.GetReference(name);
    }

    public IReferenceSchema? GetReferenceByName(string dataName, NamingConvention namingConvention)
    {
        return _instance.GetReferenceByName(dataName, namingConvention);
    }

    public IReferenceSchema GetReferenceOrThrowException(string referenceName)
    {
        return _instance.GetReferenceOrThrowException(referenceName);
    }

    public IEntityAttributeSchema? GetAttribute(string name)
    {
        return _instance.GetAttribute(name);
    }

    public IEntityAttributeSchema? GetAttributeByName(string name, NamingConvention namingConvention)
    {
        return _instance.GetAttributeByName(name, namingConvention);
    }

    public IDictionary<string, SortableAttributeCompoundSchema> GetSortableAttributeCompounds()
    {
        return _instance.GetSortableAttributeCompounds();
    }

    public SortableAttributeCompoundSchema? GetSortableAttributeCompound(string name)
    {
        return _instance.GetSortableAttributeCompound(name);
    }

    public SortableAttributeCompoundSchema? GetSortableAttributeCompoundByName(string name,
        NamingConvention namingConvention)
    {
        return _instance.GetSortableAttributeCompoundByName(name, namingConvention);
    }

    public IList<SortableAttributeCompoundSchema> GetSortableAttributeCompoundsForAttribute(string attributeName)
    {
        return _instance.GetSortableAttributeCompoundsForAttribute(attributeName);
    }

    IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.CooperatingWith(Func<CatalogSchema> catalogSupplier)
    {
        return CooperatingWith(catalogSupplier);
    }
}