using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Client.Utils;
using static EvitaDB.Client.Models.Schemas.Builders.SchemaBuilderHelper;

namespace EvitaDB.Client.Models.Schemas.Builders;

public class InternalEntitySchemaBuilder : IEntitySchemaBuilder
{
	private bool _withGeneratedPrimaryKey;
	private bool _withHierarchy;
	private bool _withPrice;
	public IEntitySchema BaseSchema { get; }
    public List<IEntitySchemaMutation> Mutations { get; } = new();
    private Func<ICatalogSchema> CatalogSchemaAccessor { get; set; }
    private bool UpdatedSchemaDirty { get; set; } = true;
    private EntitySchema? UpdatedSchema { get; set; }

    public InternalEntitySchemaBuilder(
        ICatalogSchema catalogSchema,
        IEntitySchema baseSchema,
        ICollection<IEntitySchemaMutation> schemaMutations)
    {
        CatalogSchemaAccessor = () => catalogSchema;
        BaseSchema = baseSchema;
        UpdatedSchemaDirty = AddMutations(
            CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations, schemaMutations.ToArray());
    }
    
    public InternalEntitySchemaBuilder(
        ICatalogSchema catalogSchema,
    IEntitySchema baseSchema
    ) : this(catalogSchema, baseSchema, new List<IEntitySchemaMutation>()) { }
    
    
	public InternalEntitySchemaBuilder CooperatingWith(Func<CatalogSchema> catalogSupplier) {
		CatalogSchemaAccessor = catalogSupplier;
		return this;
	}

	public IEntitySchemaBuilder VerifySchemaStrictly()
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder VerifySchemaButAllow(params EvolutionMode[] evolutionMode)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder VerifySchemaButCreateOnTheFly()
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithDescription(string? description)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder Deprecated(string deprecationNotice)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder NotDeprecatedAnymore()
	{
		throw new NotImplementedException();
	}

	IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.WithGeneratedPrimaryKey()
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithoutGeneratedPrimaryKey()
	{
		throw new NotImplementedException();
	}

	IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.WithHierarchy()
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithoutHierarchy()
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithAttribute(string attributeName, Type ofType)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithoutAttribute(string attributeName)
	{
		throw new NotImplementedException();
	}

	IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.WithPrice()
	{
		throw new NotImplementedException();
	}

	IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.WithPrice(int indexedDecimalPlaces)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithPriceInCurrency(params Currency[] currency)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithPriceInCurrency(int indexedPricePlaces, params Currency[] currency)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithoutPrice()
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithoutPriceInCurrency(Currency currency)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithLocale(params CultureInfo[] locale)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithoutLocale(CultureInfo locale)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithGlobalAttribute(string attributeName)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithAssociatedData(string dataName, Type ofType)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithoutAssociatedData(string dataName)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithReferenceTo(string name, string externalEntityType, Cardinality cardinality)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithReferenceToEntity(string name, string entityType, Cardinality cardinality)
	{
		throw new NotImplementedException();
	}

	public IEntitySchemaBuilder WithoutReferenceTo(string name)
	{
		throw new NotImplementedException();
	}

	/*public EntitySchemaBuilder VerifySchemaStrictly() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new DisallowEvolutionModeInEntitySchemaMutation(Enum.GetValues<EvolutionMode>())
		);
		return this;
	}

	public EntitySchemaBuilder VerifySchemaButAllow(params EvolutionMode[] evolutionMode) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new DisallowEvolutionModeInEntitySchemaMutation(Enum.GetValues<EvolutionMode>()),
			new AllowEvolutionModeInEntitySchemaMutation(evolutionMode)
		);
		return this;
	}
	
	public EntitySchemaBuilder VerifySchemaButCreateOnTheFly() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new AllowEvolutionModeInEntitySchemaMutation(Enum.GetValues<EvolutionMode>())
		);
		return this;
	}

	public EntitySchemaBuilder WithDescription(string? description) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new ModifyEntitySchemaDescriptionMutation(description)
		);
		return this;
	}

	public EntitySchemaBuilder Deprecated(string deprecationNotice) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new ModifyEntitySchemaDeprecationNoticeMutation(deprecationNotice)
		);
		return this;
	}

	public EntitySchemaBuilder NotDeprecatedAnymore() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new ModifyEntitySchemaDeprecationNoticeMutation(null)
		);
		return this;
	}

	public EntitySchemaBuilder WithGeneratedPrimaryKey() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new SetEntitySchemaWithGeneratedPrimaryKeyMutation(true)
		);
		return this;
	}

	public EntitySchemaBuilder WithoutGeneratedPrimaryKey() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new SetEntitySchemaWithGeneratedPrimaryKeyMutation(false)
		);
		return this;
	}

	public EntitySchemaBuilder WithHierarchy() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new SetEntitySchemaWithHierarchyMutation(true)
		);
		return this;
	}

	public EntitySchemaBuilder WithoutHierarchy() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new SetEntitySchemaWithHierarchyMutation(false)
		);
		return this;
	}

	public EntitySchemaBuilder WithPrice() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new SetEntitySchemaWithPriceMutation(true, 2)
		);
		return this;
	}

	public EntitySchemaBuilder WithPrice(int indexedDecimalPlaces) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new SetEntitySchemaWithPriceMutation(true, indexedDecimalPlaces)
		);
		return this;
	}

	public EntitySchemaBuilder WithPriceInCurrency(params Currency[] currency) {
		return WithPriceInCurrency(2, currency);
	}

	public EntitySchemaBuilder WithPriceInCurrency(int indexedPricePlaces, params Currency[] currency) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new SetEntitySchemaWithPriceMutation(true, indexedPricePlaces),
			new AllowCurrencyInEntitySchemaMutation(currency)
		);
		return this;
	}

	public EntitySchemaBuilder WithoutPrice() {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new SetEntitySchemaWithPriceMutation(false, 0)
		);
		return this;
	}

	public EntitySchemaBuilder WithoutPriceInCurrency(Currency currency) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new DisallowCurrencyInEntitySchemaMutation(currency)
		);
		return this;
	}

	public EntitySchemaBuilder WithLocale(params CultureInfo[] locale) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new AllowLocaleInEntitySchemaMutation(locale)
		);
		return this;
	}

	public EntitySchemaBuilder WithoutLocale(CultureInfo locale) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new DisallowLocaleInEntitySchemaMutation(locale)
		);
		return this;
	}

	public EntitySchemaBuilder WithGlobalAttribute(string attributeName) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new UseGlobalAttributeSchemaMutation(attributeName)
		);
		return this;
	}

	public EntitySchemaBuilder WithAttribute(
		string attributeName,
		Type ofType,
		Action<AttributeSchemaBuilder>? whichIs = null) {
		CatalogSchema catalogSchema = CatalogSchemaAccessor.Invoke();
		GlobalAttributeSchema? existingAttributeSchema = catalogSchema.GetAttribute(attributeName);
		if (existingAttributeSchema is not null)
		{
			throw new AttributeAlreadyPresentInCatalogSchemaException(
				catalogSchema.Name, existingAttributeSchema
			);
		}
		AttributeSchema? existingAttribute = BaseSchema.GetAttribute(attributeName);
		AttributeSchemaBuilder attributeSchemaBuilder =
			existingAttribute
				.map(it => {
					Assert.IsTrue(
						ofType.Equals(it.getType()),
						() => new InvalidSchemaMutationException(
							"Attribute " + attributeName + " has already assigned type " + it.getType() +
								", cannot change this type to: " + ofType + "!"
						)
					);
					return new AttributeSchemaBuilder(BaseSchema, it);
				})
				.orElseGet(() => new AttributeSchemaBuilder(BaseSchema, attributeName, ofType));

		if (whichIs is not null)
		{
			whichIs!.Invoke(attributeSchemaBuilder);
		}
		AttributeSchema attributeSchema = attributeSchemaBuilder.toInstance();
		CheckSortableTraits(attributeName, attributeSchema);

		// check the names in all naming conventions are unique in the catalog schema
		CheckNamesAreUniqueInAllNamingConventions(BaseSchema.Attributes.Values, attributeSchema);

		if (existingAttribute.map(it => !it.equals(attributeSchema)).orElse(true)) {
			UpdatedSchemaDirty = AddMutations(
				catalogSchema, BaseSchema, Mutations,
				attributeSchemaBuilder.toMutation().toArray()
			);
		}
		return this;
	}

	public EntitySchemaBuilder WithoutAttribute(string attributeName) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new RemoveAttributeSchemaMutation(attributeName)
		);
		return this;
	}

	public EntitySchemaBuilder WithAssociatedData(string dataName, Type ofType) {
		return WithAssociatedData(dataName, ofType, null);
	}

	public EntitySchemaBuilder WithAssociatedData(
		string dataName,
		Type ofType,
		Action<AssociatedDataSchemaEditor> whichIs
	) {
		AssociatedDataSchema? existingAssociatedData = BaseSchema.GetAssociatedData(dataName);
		CatalogSchema catalogSchema = CatalogSchemaAccessor.Invoke();
		AssociatedDataSchemaBuilder associatedDataSchemaBuilder = existingAssociatedData
			.map(it => {
				Assert.IsTrue(
					ofType.Equals(it.Type),
					() => new InvalidSchemaMutationException(
						"Associated data " + dataName + " has already assigned type " + it.getType() +
							", cannot change this type to: " + ofType + "!"
					)
				);
				return new AssociatedDataSchemaBuilder(catalogSchema, BaseSchema, it);
			})
			.orElseGet(() => new AssociatedDataSchemaBuilder(catalogSchema, BaseSchema, dataName, ofType));

		if (whichIs != null) {
			whichIs.Invoke(associatedDataSchemaBuilder);
		}
		AssociatedDataSchema associatedDataSchema = associatedDataSchemaBuilder.toInstance();

		if (existingAssociatedData is not null && !existingAssociatedData.Equals(associatedDataSchema)) { 
			ClassifierUtils.ValidateClassifierFormat(ClassifierType.AssociatedData, dataName);
			// check the names in all naming conventions are unique in the entity schema
			BaseSchema.AssociatedData
				.Values
				.Where(it => !Equals(it.Name, associatedDataSchema.Name))
				.SelectMany(it => it.NameVariants
					.Where(nameVariant => nameVariant.Value.Equals(associatedDataSchema.GetNameVariant(nameVariant.Key)))
					.Select(nameVariant => new AssociatedDataNamingConventionConflict(it, nameVariant.Key, nameVariant.Value))
				).ToList()
				.ForEach(conflict => throw new AssociatedDataAlreadyPresentInEntitySchemaException(
					conflict.ConflictingSchema, associatedDataSchema,
					conflict.Convention, conflict.ConflictingName
				));
			UpdatedSchemaDirty = AddMutations(
				catalogSchema, BaseSchema, Mutations,
				associatedDataSchemaBuilder.toMutation().toArray()
			);
		}
		return this;
	}

	public EntitySchemaBuilder WithoutAssociatedData(string dataName) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new RemoveAssociatedDataSchemaMutation(dataName)
		);
		return this;
	}

	public EntitySchemaBuilder WithReferenceTo(string name, string externalEntityType, Cardinality cardinality) {
		return WithReferenceTo(name, externalEntityType, cardinality, null);
	}

	public EntitySchemaBuilder WithReferenceTo(
		string name,
		string externalEntityType,
		Cardinality cardinality,
		Action<ReferenceSchemaBuilder> whichIs
	) {
		EntitySchema currentSchema = ToInstance();
		ReferenceSchema? existingReference = currentSchema.GetReference(name);
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
		if (whichIs != null) {
			whichIs.Invoke(referenceBuilder);
		}
		RedefineReferenceType(
			referenceBuilder,
			existingReference
		);
		return this;
	}

	public EntitySchemaBuilder WithReferenceToEntity(string name, string entityType, Cardinality cardinality) {
		return WithReferenceToEntity(name, entityType, cardinality, null);
	}

	public EntitySchemaBuilder WithReferenceToEntity(
		string name,
		string entityType,
		Cardinality cardinality,
		Action<ReferenceSchemaBuilder> whichIs
	) {
		EntitySchema currentSchema = ToInstance();
		ReferenceSchema? existingReference = currentSchema.GetReference(name);
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
		if (whichIs != null) {
			whichIs.Invoke(referenceSchemaBuilder);
		}
		RedefineReferenceType(
			referenceSchemaBuilder,
			existingReference
		);
		return this;
	}
	
	public EntitySchemaBuilder WithoutReferenceTo(string name) {
		UpdatedSchemaDirty = AddMutations(
			CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
			new RemoveReferenceSchemaMutation(name)
		);
		return this;
	}

	public ModifyEntitySchemaMutation? ToMutation() {
		return Mutations.Any() ? new ModifyEntitySchemaMutation(BaseSchema.Name, Mutations.ToArray()) : null;
	}
	
	public EntitySchema ToInstance() {
		if (UpdatedSchema == null || UpdatedSchemaDirty) {
			EntitySchema? currentSchema = BaseSchema;
			foreach (IEntitySchemaMutation mutation in Mutations) {
				currentSchema = mutation.Mutate(CatalogSchemaAccessor.Invoke(), currentSchema);
				if (currentSchema == null) {
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
		ReferenceSchema? existingReference
	) {
		ReferenceSchema newReference = referenceSchemaBuilder.toInstance();
		if (!Equals(existingReference, newReference)) {
			// remove all existing mutations for the reference schema (it needs to be replaced)
			Mutations.RemoveAll(it =>
				it is ReferenceSchemaMutation referenceSchemaMutation &&
				referenceSchemaMutation.getName().equals(newReference.Name));
			// check the names in all naming conventions are unique in the entity schema
			ToInstance()
				.References
				.Values
				.Where(it => !Equals(it.Name, referenceSchemaBuilder.Name))
				.SelectMany(
					it => it.NameVariants
					.Where(nameVariant => nameVariant.Value.Equals(referenceSchemaBuilder.getNameVariant(nameVariant.Key)))
					.Select(nameVariant => new ReferenceNamingConventionConflict(it, nameVariant.Key, nameVariant.Value))
				)
				.ToList()
				.ForEach(conflict => throw new ReferenceAlreadyPresentInEntitySchemaException(
					conflict.ConflictingSchema, referenceSchemaBuilder,
					conflict.Convention, conflict.ConflictingName
				));
			UpdatedSchemaDirty = AddMutations(
				CatalogSchemaAccessor.Invoke(), BaseSchema, Mutations,
				referenceSchemaBuilder.toMutation().toArray()
			);
		}
	}*/


    /**
	 * DTO for passing the identified conflict in attribute names for certain naming convention.
	 */
    public record AttributeNamingConventionConflict(AttributeSchema ConflictingSchema, NamingConvention Convention,
        string ConflictingName);

    /**
	 * DTO for passing the identified conflict in associated data names for certain naming convention.
	 */
    public record AssociatedDataNamingConventionConflict(AssociatedDataSchema ConflictingSchema, NamingConvention Convention,
        string ConflictingName
    );

    /**
	 * DTO for passing the identified conflict in reference names for certain naming convention.
	 */
    public record ReferenceNamingConventionConflict(ReferenceSchema ConflictingSchema, NamingConvention Convention,
        string ConflictingName
    );

    public int Version { get; }
    public string Name { get; }
    public string? Description { get; }
    public IDictionary<NamingConvention, string> NameVariants { get; }
    public string GetNameVariant(NamingConvention namingConvention)
    {
	    throw new NotImplementedException();
    }

    public string? DeprecationNotice { get; }
    public IDictionary<string, IAttributeSchema> GetAttributes()
    {
	    throw new NotImplementedException();
    }

    IAttributeSchema? IEntitySchema.GetAttribute(string name)
    {
	    return GetAttribute(name);
    }

    IAttributeSchema? IEntitySchema.GetAttributeByName(string dataName, NamingConvention namingConvention)
    {
	    return GetAttributeByName(dataName, namingConvention);
    }

    public bool DiffersFrom(IEntitySchema? otherSchema)
    {
	    throw new NotImplementedException();
    }

    public ISet<EvolutionMode> GetEvolutionMode()
    {
	    throw new NotImplementedException();
    }

    public bool Allows(EvolutionMode evolutionMode)
    {
	    throw new NotImplementedException();
    }

    public bool SupportsLocale(CultureInfo locale)
    {
	    throw new NotImplementedException();
    }

    public IAttributeSchema GetAttributeOrThrow(string name)
    {
	    throw new NotImplementedException();
    }

    bool IEntitySchema.WithGeneratedPrimaryKey => _withGeneratedPrimaryKey;

    bool IEntitySchema.WithHierarchy => _withHierarchy;

    bool IEntitySchema.WithPrice => _withPrice;

    public int IndexedPricePlaces { get; }
    public ISet<CultureInfo> Locales { get; }
    public ISet<Currency> Currencies { get; }
    public ISet<EvolutionMode> EvolutionModes { get; }
    public IEnumerable<IAttributeSchema> NonNullableAttributes { get; }
    public IEnumerable<IAssociatedDataSchema> NonNullableAssociatedData { get; }
    public IDictionary<string, IAttributeSchema> Attributes { get; }
    public IDictionary<string, IAssociatedDataSchema> AssociatedData { get; }
    public IDictionary<string, IReferenceSchema> References { get; }
    public bool IsBlank()
    {
	    throw new NotImplementedException();
    }

    public IAssociatedDataSchema? GetAssociatedData(string name)
    {
	    throw new NotImplementedException();
    }

    public IAssociatedDataSchema GetAssociatedDataOrThrow(string name)
    {
	    throw new NotImplementedException();
    }

    public IAssociatedDataSchema? GetAssociatedDataByName(string dataName, NamingConvention namingConvention)
    {
	    throw new NotImplementedException();
    }

    public IReferenceSchema? GetReference(string name)
    {
	    throw new NotImplementedException();
    }

    public IReferenceSchema? GetReferenceByName(string dataName, NamingConvention namingConvention)
    {
	    throw new NotImplementedException();
    }

    public IReferenceSchema GetReferenceOrThrowException(string referenceName)
    {
	    throw new NotImplementedException();
    }

    public IAttributeSchema? GetAttribute(string name)
    {
	    throw new NotImplementedException();
    }

    public IAttributeSchema? GetAttributeByName(string name, NamingConvention namingConvention)
    {
	    throw new NotImplementedException();
    }

    public IDictionary<string, SortableAttributeCompoundSchema> GetSortableAttributeCompounds()
    {
	    throw new NotImplementedException();
    }

    public SortableAttributeCompoundSchema? GetSortableAttributeCompound(string name)
    {
	    throw new NotImplementedException();
    }

    public SortableAttributeCompoundSchema? GetSortableAttributeCompoundByName(string name, NamingConvention namingConvention)
    {
	    throw new NotImplementedException();
    }

    public IList<SortableAttributeCompoundSchema> GetSortableAttributeCompoundsForAttribute(string attributeName)
    {
	    throw new NotImplementedException();
    }

    IEntitySchemaBuilder IEntitySchemaEditor<IEntitySchemaBuilder>.CooperatingWith(Func<CatalogSchema> catalogSupplier)
    {
	    return CooperatingWith(catalogSupplier);
    }

    public ModifyEntitySchemaMutation? ToMutation()
    {
	    throw new NotImplementedException();
    }

    public EntitySchema ToInstance()
    {
	    throw new NotImplementedException();
    }
}