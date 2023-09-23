using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.References;
using EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Builders;

public class ReferenceSchemaBuilder : IReferenceSchemaBuilder
{
	private ICatalogSchema CatalogSchema { get; }
    private IEntitySchema EntitySchema { get; }
    private IReferenceSchema BaseSchema { get; }
    private IList<IEntitySchemaMutation> Mutations { get; } = new List<IEntitySchemaMutation>();
    private bool UpdatedSchemaDirty { get; set; }
    private IReferenceSchema? UpdatedSchema { get; set; }
    public string Name => _instance.Name;
    public string? Description => _instance.Description;
    public IDictionary<NamingConvention, string> NameVariants => _instance.NameVariants;
    public string? DeprecationNotice => _instance.DeprecationNotice;
    public Cardinality Cardinality => _instance.Cardinality;
    public string ReferencedEntityType => _instance.ReferencedEntityType;
    public string? ReferencedGroupType => _instance.ReferencedGroupType;
    public bool ReferencedEntityTypeManaged => _instance.ReferencedEntityTypeManaged;
    public bool ReferencedGroupTypeManaged => _instance.ReferencedGroupTypeManaged;
    public bool IsIndexed => _instance.IsIndexed;
    public bool IsFaceted => _instance.IsFaceted;
    
    private readonly IReferenceSchema _instance;

    internal ReferenceSchemaBuilder(
		ICatalogSchema catalogSchema,
		IEntitySchema entitySchema,
		IReferenceSchema? existingSchema,
		string name,
		string entityType,
		bool entityTypeRelatesToEntity,
		Cardinality cardinality,
		IList<IEntitySchemaMutation> mutations,
		bool createNew
	) {
		CatalogSchema = catalogSchema;
		EntitySchema = entitySchema;
		BaseSchema = existingSchema ?? ReferenceSchema.InternalBuild(
			name, entityType, entityTypeRelatesToEntity, cardinality, null, false, false, false
		);
		if (createNew) {
			Mutations.Add(
				new CreateReferenceSchemaMutation(
					BaseSchema.Name,
					BaseSchema.Description,
					BaseSchema.DeprecationNotice,
					cardinality,
					entityType,
					entityTypeRelatesToEntity,
					BaseSchema.ReferencedGroupType,
					BaseSchema.ReferencedGroupTypeManaged,
					BaseSchema.IsIndexed,
					BaseSchema.IsFaceted
				)
			);
		}

		foreach (var mutation in mutations
			         .Where(it => it is IReferenceSchemaMutation referenceSchemaMutation &&
			                      name.Equals(referenceSchemaMutation.Name) && referenceSchemaMutation is not CreateReferenceSchemaMutation))
		{
			Mutations.Add(mutation);
		}
		
		_instance ??= ToInstance();
	}

	public IReferenceSchemaBuilder WithDescription(string? description) {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema,Mutations,
			new ModifyReferenceSchemaDescriptionMutation(Name, description)
		);
		return this;
	}

	public IReferenceSchemaBuilder Deprecated(string deprecationNotice) {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new ModifyReferenceSchemaDeprecationNoticeMutation(Name, deprecationNotice)
		);
		return this;
	}

	public IReferenceSchemaBuilder NotDeprecatedAnymore() {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new ModifyReferenceSchemaDeprecationNoticeMutation(Name, null)
		);
		return this;
	}

	public IReferenceSchemaBuilder WithGroupType(string groupType) {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new ModifyReferenceSchemaRelatedEntityGroupMutation(Name, groupType, false)
		);
		return this;
	}

	public IReferenceSchemaBuilder WithGroupTypeRelatedToEntity(string groupType) {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new ModifyReferenceSchemaRelatedEntityGroupMutation(Name, groupType, true)
		);
		return this;
	}

	public IReferenceSchemaBuilder WithoutGroupType() {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new ModifyReferenceSchemaRelatedEntityGroupMutation(Name, null, false)
		);
		return this;
	}

	public IReferenceSchemaBuilder Indexed() {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new SetReferenceSchemaIndexedMutation(Name, true)
		);
		return this;
	}

	public IReferenceSchemaBuilder NonIndexed() {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new SetReferenceSchemaIndexedMutation(Name, false)
		);
		return this;
	}

	public IReferenceSchemaBuilder Faceted() {
		if (ToInstance().IsIndexed) {
			UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
				CatalogSchema, EntitySchema, Mutations,
				new SetReferenceSchemaFacetedMutation(Name, true)
			);
		} else {
			UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
				CatalogSchema, EntitySchema, Mutations,
				new SetReferenceSchemaIndexedMutation(Name, true),
				new SetReferenceSchemaFacetedMutation(Name, true)
			);
		}
		return this;
	}

	public IReferenceSchemaBuilder NonFaceted() {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new SetReferenceSchemaFacetedMutation(Name, false)
		);
		return this;
	}

	public IReferenceSchemaBuilder WithAttribute<T>(string attributeName) {
		return WithAttribute<T>(attributeName, null);
	}

	public IReferenceSchemaBuilder WithAttribute<T>(
		string attributeName,
		Action<IAttributeSchemaBuilder>? whichIs
	) {
		IAttributeSchema? existingAttribute = GetAttribute(attributeName);
		AttributeSchemaBuilder attributeSchemaBuilder;
		if (existingAttribute is not null)
		{
			Assert.IsTrue(
				typeof(T) == existingAttribute.GetType(),
				() => new InvalidSchemaMutationException(
					"Attribute " + attributeName + " has already assigned type " + existingAttribute.GetType() +
					", cannot change this type to: " + typeof(T) + "!"
				)
			);
			attributeSchemaBuilder = new AttributeSchemaBuilder(EntitySchema, existingAttribute);
		}
		else
		{
			attributeSchemaBuilder = new AttributeSchemaBuilder(EntitySchema, attributeName, typeof(T));
		}

		whichIs?.Invoke(attributeSchemaBuilder);
		IAttributeSchema attributeSchema = attributeSchemaBuilder.ToInstance();
		SchemaBuilderHelper.CheckSortableTraits(attributeName, attributeSchema);

		// check the names in all naming conventions are unique in the catalog schema
		SchemaBuilderHelper.CheckNamesAreUniqueInAllNamingConventions(
			GetAttributes().Values,
			GetSortableAttributeCompounds().Values,
			attributeSchema
		);

		if (existingAttribute is not null && !existingAttribute.Equals(attributeSchema) || true)
		{
			UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
				CatalogSchema, EntitySchema, Mutations,
				attributeSchemaBuilder
					.ToReferenceMutation(Name)
					.Select(it => (IEntitySchemaMutation) it)
					.ToArray()
			);
		}
		return this;
	}

	public IReferenceSchemaBuilder WithoutAttribute(string attributeName) {
		SchemaBuilderHelper.CheckSortableAttributeCompoundsWithoutAttribute(
			attributeName, GetSortableAttributeCompounds().Values
		);
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new ModifyReferenceAttributeSchemaMutation(
				Name,
				new RemoveAttributeSchemaMutation(attributeName)
			)
		);
		return this;
	}

	public IReferenceSchemaBuilder WithSortableAttributeCompound(
		string name,
		params AttributeElement[] attributeElements
	) {
		return WithSortableAttributeCompound(
			name, attributeElements, null
		);
	}

	public IReferenceSchemaBuilder WithSortableAttributeCompound(
		string name,
		AttributeElement[] attributeElements,
		Action<SortableAttributeCompoundSchemaBuilder>? whichIs
	) {
		ISortableAttributeCompoundSchema? existingCompound = GetSortableAttributeCompound(name);
		ISortableAttributeCompoundSchemaBuilder builder = new SortableAttributeCompoundSchemaBuilder(
			CatalogSchema,
			EntitySchema,
			this,
			BaseSchema.GetSortableAttributeCompound(name),
			name,
			attributeElements.ToList(),
			new List<IEntitySchemaMutation>(),
			true
		);
		SortableAttributeCompoundSchemaBuilder schemaBuilder = new SortableAttributeCompoundSchemaBuilder(
			CatalogSchema,
			EntitySchema,
			this,
			BaseSchema.GetSortableAttributeCompound(name),
			name,
			attributeElements.ToList(),
			new List<IEntitySchemaMutation>(),
			true
		);
		if (existingCompound is not null)
		{
			Assert.IsTrue(
				existingCompound.AttributeElements.Equals(attributeElements.ToList()),
				() => new AttributeAlreadyPresentInEntitySchemaException(
					existingCompound, builder.ToInstance(), null, name
				)
				);
		}
		
		whichIs?.Invoke(schemaBuilder);
		ISortableAttributeCompoundSchema compoundSchema = schemaBuilder.ToInstance();
		Assert.IsTrue(
			compoundSchema.AttributeElements.Count > 1,
			() => new SortableAttributeCompoundSchemaException(
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
			() => new SortableAttributeCompoundSchemaException(
				"Attribute names of elements in sortable attribute compound must be unique!",
				compoundSchema
			)
		);
		SchemaBuilderHelper.CheckSortableTraits(Name, compoundSchema, GetAttributes());

		// check the names in all naming conventions are unique in the catalog schema
		SchemaBuilderHelper.CheckNamesAreUniqueInAllNamingConventions(
			GetAttributes().Values,
			GetSortableAttributeCompounds().Values,
			compoundSchema
		);

		if (existingCompound is not null && !existingCompound.Equals(compoundSchema) || true)
		{
			UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
				CatalogSchema, EntitySchema, Mutations,
				schemaBuilder
					.ToReferenceMutation(Name)
					.Select(it => (IEntitySchemaMutation) it)
					.ToArray()
			);
		}
		
		return this;
	}

	public IReferenceSchemaBuilder WithoutSortableAttributeCompound(string name) {
		UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
			CatalogSchema, EntitySchema, Mutations,
			new ModifyReferenceSortableAttributeCompoundSchemaMutation(
				Name,
				new RemoveSortableAttributeCompoundSchemaMutation(name)
			)
		);
		return this;
	}
	
	public IReferenceSchema ToInstance() {
		if (UpdatedSchema == null || UpdatedSchemaDirty) {
			IReferenceSchema? currentSchema = BaseSchema;
			foreach (IEntitySchemaMutation mutation in Mutations) {
				currentSchema = ((IReferenceSchemaMutation)mutation).Mutate(EntitySchema, currentSchema);
				if (currentSchema == null) {
					throw new EvitaInternalError("Attribute unexpectedly removed from inside!");
				}
			}
			UpdatedSchema = currentSchema;
			UpdatedSchemaDirty = false;
		}
		return UpdatedSchema;
	}

	public ICollection<IEntitySchemaMutation> ToMutation() {
		return Mutations;
	}
	
	public string GetNameVariant(NamingConvention namingConvention)
	{
		return _instance.GetNameVariant(namingConvention);
	}

	public IDictionary<string, IAttributeSchema> GetAttributes()
	{
		return _instance.GetAttributes();
	}

	public IAttributeSchema? GetAttribute(string name)
	{
		return _instance.GetAttribute(name);
	}

	public IAttributeSchema? GetAttributeByName(string name, NamingConvention namingConvention)
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

	public SortableAttributeCompoundSchema? GetSortableAttributeCompoundByName(string name, NamingConvention namingConvention)
	{
		return _instance.GetSortableAttributeCompoundByName(name, namingConvention);
	}

	public IList<SortableAttributeCompoundSchema> GetSortableAttributeCompoundsForAttribute(string attributeName)
	{
		return _instance.GetSortableAttributeCompoundsForAttribute(attributeName);
	}

	public IDictionary<NamingConvention, string> GetEntityTypeNameVariants(Func<string, EntitySchema> entitySchemaFetcher)
	{
		return _instance.GetEntityTypeNameVariants(entitySchemaFetcher);
	}

	public string GetReferencedEntityTypeNameVariants(NamingConvention namingConvention, Func<string, EntitySchema> entitySchemaFetcher)
	{
		return _instance.GetReferencedEntityTypeNameVariants(namingConvention, entitySchemaFetcher);
	}

	public IDictionary<NamingConvention, string> GetGroupTypeNameVariants(Func<string, EntitySchema> entitySchemaFetcher)
	{
		return _instance.GetGroupTypeNameVariants(entitySchemaFetcher);
	}

	public string GetReferencedGroupTypeNameVariants(NamingConvention namingConvention, Func<string, EntitySchema> entitySchemaFetcher)
	{
		return _instance.GetReferencedGroupTypeNameVariants(namingConvention, entitySchemaFetcher);
	}
}