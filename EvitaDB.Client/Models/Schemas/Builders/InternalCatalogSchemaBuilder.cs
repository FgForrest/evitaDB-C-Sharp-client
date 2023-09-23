using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Builders;

public class InternalCatalogSchemaBuilder : ICatalogSchemaBuilder
{
    private ICatalogSchema BaseSchema { get; }
    private IList<ILocalCatalogSchemaMutation> Mutations { get; } = new List<ILocalCatalogSchemaMutation>();
    private bool UpdatedSchemaDirty { get; set; }
    private ICatalogSchema? UpdatedSchema { get; set; }

    public string Name => _instance.Name;
    public string? Description => _instance.Description;
    public IDictionary<NamingConvention, string> NameVariants => _instance.NameVariants;

    public int Version => _instance.Version;

    private readonly ICatalogSchema _instance;

    public InternalCatalogSchemaBuilder(ICatalogSchema baseSchema,
        IEnumerable<ILocalCatalogSchemaMutation> schemaMutations)
    {
        BaseSchema = baseSchema;
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            BaseSchema, Mutations,
            schemaMutations.ToArray()
        );
        _instance ??= ToInstance();
    }

    public InternalCatalogSchemaBuilder(ICatalogSchema baseSchema) : this(baseSchema,
        new List<ILocalCatalogSchemaMutation>())
    {
    }

    public ICatalogSchemaBuilder WithDescription(string? description)
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            BaseSchema, Mutations,
            new ModifyCatalogSchemaDescriptionMutation(description)
        );
        return this;
    }

    public ICatalogSchemaBuilder VerifyCatalogSchemaStrictly()
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            BaseSchema, Mutations,
            new DisallowEvolutionModeInCatalogSchemaMutation(Enum.GetValues<CatalogEvolutionMode>())
        );
        return this;
    }

    public ICatalogSchemaBuilder VerifyCatalogSchemaButCreateOnTheFly()
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            BaseSchema, Mutations,
            new AllowEvolutionModeInCatalogSchemaMutation(Enum.GetValues<CatalogEvolutionMode>())
        );
        return this;
    }

    public ICatalogSchemaBuilder WithEntitySchema(string entityType, Action<IEntitySchemaBuilder>? whichIs)
    {
        IEntitySchema? entitySchema = BaseSchema.GetEntitySchema(entityType);
        if (entitySchema is not null)
        {
            IEntitySchemaBuilder existingBuilder =
                new InternalEntitySchemaBuilder(ToInstance(), entitySchema).CooperatingWith(() => this);
            whichIs?.Invoke(existingBuilder);
            ModifyEntitySchemaMutation? mutation = existingBuilder.ToMutation();
            if (mutation is not null)
            {
                UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
                    BaseSchema, Mutations,
                    mutation
                );
            }
        }
        else
        {
            IEntitySchemaBuilder notExistingBuilder =
                new InternalEntitySchemaBuilder(ToInstance(), EntitySchema.InternalBuild(entityType)).CooperatingWith(
                    () => this);
            whichIs?.Invoke(notExistingBuilder);
            ModifyEntitySchemaMutation? mutation = notExistingBuilder.ToMutation();
            CreateEntitySchemaMutation createEntitySchemaMutation = new CreateEntitySchemaMutation(entityType);

            UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
                BaseSchema, Mutations,
                mutation is null
                    ? new ILocalCatalogSchemaMutation[] {createEntitySchemaMutation}
                    : new ILocalCatalogSchemaMutation[] {createEntitySchemaMutation, mutation}
            );
        }

        return this;
    }

    public ICatalogSchemaBuilder WithAttribute<TT>(string attributeName)
    {
        return WithAttribute<TT>(attributeName, null);
    }

    public ICatalogSchemaBuilder WithAttribute<TT>(string attributeName,
        Action<IGlobalAttributeSchemaBuilder>? whichIs)
    {
        IGlobalAttributeSchema? existingAttribute = GetAttribute(attributeName);
        Assert.IsTrue(
            typeof(TT) == existingAttribute?.GetType(),
            () => new InvalidSchemaMutationException(
                "Attribute " + attributeName + " has already assigned type " + existingAttribute?.GetType() +
                ", cannot change this type to: " + typeof(TT) + "!"
            )
        );
        GlobalAttributeSchemaBuilder attributeSchemaBuilder = existingAttribute is not null
            ? new GlobalAttributeSchemaBuilder(BaseSchema, existingAttribute)
            : new GlobalAttributeSchemaBuilder(BaseSchema, attributeName, typeof(TT));

        whichIs?.Invoke(attributeSchemaBuilder);
        IGlobalAttributeSchema attributeSchema = attributeSchemaBuilder.ToInstance();
        SchemaBuilderHelper.CheckSortableTraits(attributeName, attributeSchema);

        // check the names in all naming conventions are unique in the catalog schema
        SchemaBuilderHelper.CheckNamesAreUniqueInAllNamingConventions(
            GetAttributes().Values as ICollection<IAttributeSchema> ?? throw new InvalidOperationException(),
            new List<SortableAttributeCompoundSchema>(),
            attributeSchema
        );

        if (existingAttribute is not null && existingAttribute.Equals(attributeSchema) || true)
        {
            UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
                BaseSchema, Mutations,
                attributeSchemaBuilder.ToMutation().ToArray()
            );
        }

        return this;
    }

    public ICatalogSchemaBuilder WithoutAttribute(string attributeName)
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            BaseSchema, Mutations,
            new RemoveAttributeSchemaMutation(attributeName)
        );
        return this;
    }

    public ModifyCatalogSchemaMutation? ToMutation()
    {
        return !Mutations.Any() ? null : new ModifyCatalogSchemaMutation(Name, Mutations.ToArray());
    }

    public ICatalogSchema ToInstance()
    {
        if (UpdatedSchema == null || UpdatedSchemaDirty)
        {
            ICatalogSchema? currentSchema = BaseSchema;
            foreach (ILocalCatalogSchemaMutation mutation in Mutations)
            {
                currentSchema = mutation.Mutate(currentSchema);
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

    public string GetNameVariant(NamingConvention namingConvention) => ToInstance().GetNameVariant(namingConvention);

    public bool DiffersFrom(ICatalogSchema? otherObject) => otherObject?.DiffersFrom(this) ?? true;

    public IDictionary<string, IGlobalAttributeSchema> GetAttributes() => _instance.GetAttributes();

    public IGlobalAttributeSchema? GetAttribute(string name) => _instance.GetAttribute(name);

    public IGlobalAttributeSchema? GetAttributeByName(string name, NamingConvention namingConvention) =>
        _instance.GetAttributeByName(name, namingConvention);

    public ISet<CatalogEvolutionMode> CatalogEvolutionModes => _instance.CatalogEvolutionModes;
    public IEntitySchema? GetEntitySchema(string entityType) => _instance.GetEntitySchema(entityType);
}