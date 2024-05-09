using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Builders;

public abstract class AbstractAttributeSchemaBuilder<TE, TS> : IAttributeSchemaEditor<TE>
    where TE : class, IAttributeSchemaEditor<TE>
    where TS : class, IAttributeSchema
{
    protected TS BaseSchema { get; }
    protected ICatalogSchema? CatalogSchema { get; }
    protected IEntitySchema? EntitySchema { get; }

    protected bool UpdatedSchemaDirty { get; set; }
    protected TS? UpdatedSchema { get; set; }

    private readonly TS _instance;

    protected internal AbstractAttributeSchemaBuilder(
        ICatalogSchema? catalogSchema,
        IEntitySchema? entitySchema,
        TS existingSchema
    )
    {
        Assert.IsTrue(
            EvitaDataTypes.IsSupportedTypeOrItsArray(existingSchema.Type),
            "Data type " + existingSchema.Type.Name + " is not supported."
        );
        Assert.IsTrue(catalogSchema != null || entitySchema != null,
            "Either catalog name or entity type must be present!");
        Assert.IsTrue(!(catalogSchema != null && entitySchema != null),
            "Either catalog name or entity type must be present, but not both!");
        CatalogSchema = catalogSchema;
        EntitySchema = entitySchema;
        BaseSchema = existingSchema;
        _instance ??= ToInstance();
    }
    
    public string Name => _instance.Name;
    public string? Description => _instance.Description;
    public IDictionary<NamingConvention, string?> NameVariants => _instance.NameVariants;

    public string? GetNameVariant(NamingConvention namingConvention) => NameVariants.TryGetValue(namingConvention, out string? name) ? name : null;

    public string? DeprecationNotice => _instance.DeprecationNotice;

    bool IAttributeSchema.Unique() => _instance.Unique();
    public abstract bool UniqueWithinLocale();
    public abstract AttributeUniquenessType UniquenessType { get; }

    bool IAttributeSchema.Nullable() => _instance.Nullable();

    bool IAttributeSchema.Filterable() => _instance.Filterable();

    bool IAttributeSchema.Sortable() => _instance.Sortable();

    bool IAttributeSchema.Localized() => _instance.Localized();

    public Type Type => _instance.Type;
    public Type PlainType => _instance.PlainType;
    public object? DefaultValue => _instance.DefaultValue;
    public int IndexedDecimalPlaces => _instance.IndexedDecimalPlaces;

    public TE WithDescription(string? description)
    {
        UpdatedSchemaDirty = AddMutations(
            new ModifyAttributeSchemaDescriptionMutation(
                BaseSchema.Name,
                description
            )
        );
        return (this as TE)!;
    }
    
    public TE Deprecated(string deprecationNotice)
    {
        UpdatedSchemaDirty = AddMutations(
            new ModifyAttributeSchemaDeprecationNoticeMutation(
                BaseSchema.Name,
                deprecationNotice
            )
        );
        return (this as TE)!;
    }

    public TE NotDeprecatedAnymore()
    {
        UpdatedSchemaDirty = AddMutations(
            new ModifyAttributeSchemaDeprecationNoticeMutation(
                BaseSchema.Name,
                null
            )
        );
        return (this as TE)!;
    }

    public TE WithDefaultValue(object? defaultValue)
    {
        if (defaultValue != null)
        {
            TS currentSchema = ToInstance();
            Type expectedType = currentSchema.Type;

            UpdatedSchemaDirty = AddMutations(
                new ModifyAttributeSchemaDefaultValueMutation(
                    BaseSchema.Name,
                    EvitaDataTypes.ToTargetType(defaultValue, expectedType, currentSchema.IndexedDecimalPlaces)
                )
            );
        }
        else
        {
            UpdatedSchemaDirty = AddMutations(
                new ModifyAttributeSchemaDefaultValueMutation(
                    BaseSchema.Name,
                    null
                )
            );
        }

        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Filterable()
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaFilterableMutation(
                BaseSchema.Name,
                true
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Filterable(Func<bool> decider)
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaFilterableMutation(
                BaseSchema.Name,
                decider.Invoke()
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Unique()
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaUniqueMutation(
                BaseSchema.Name,
                AttributeUniquenessType.UniqueWithinCollection
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Unique(Func<bool> decider)
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaUniqueMutation(
                BaseSchema.Name,
                decider.Invoke() ?
                    AttributeUniquenessType.UniqueWithinCollection : AttributeUniquenessType.NotUnique
            )
        );
        return (this as TE)!;
    }
    
    TE IAttributeSchemaEditor<TE>.UniqueWithinLocale()
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaUniqueMutation(
                BaseSchema.Name,
                AttributeUniquenessType.UniqueWithinCollectionLocale
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.UniqueWithinLocale(Func<bool> decider)
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaUniqueMutation(
                BaseSchema.Name,
                decider.Invoke() ?
                    AttributeUniquenessType.UniqueWithinCollectionLocale : AttributeUniquenessType.NotUnique
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Sortable()
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaSortableMutation(
                BaseSchema.Name,
                true
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Sortable(Func<bool> decider)
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaSortableMutation(
                BaseSchema.Name,
                decider.Invoke()
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Localized()
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaLocalizedMutation(
                BaseSchema.Name,
                true
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Localized(Func<bool> decider)
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaLocalizedMutation(
                BaseSchema.Name,
                decider.Invoke()
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Nullable()
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaNullableMutation(
                BaseSchema.Name,
                true
            )
        );
        return (this as TE)!;
    }

    TE IAttributeSchemaEditor<TE>.Nullable(Func<bool> decider)
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaNullableMutation(
                BaseSchema.Name,
                decider.Invoke()
            )
        );
        return (this as TE)!;
    }

    public TE IndexDecimalPlaces(int indexedDecimalPlaces)
    {
        UpdatedSchemaDirty = AddMutations(
            new ModifyAttributeSchemaTypeMutation(
                BaseSchema.Name,
                ToAttributeMutation()
                    .Where(it => it is ModifyAttributeSchemaTypeMutation)
                    .Select(it => ((ModifyAttributeSchemaTypeMutation)it).Type)
                    .FirstOrDefault(BaseSchema.Type),
                indexedDecimalPlaces
            )
        );
        return (this as TE)!;
    }

    /// <summary>
    /// Creates attribute schema instance.
    /// </summary>
    public TS ToInstance()
    {
        if (UpdatedSchema == null || UpdatedSchemaDirty)
        {
            TS? currentSchema = BaseSchema;
            foreach (IAttributeSchemaMutation mutation in ToAttributeMutation())
            {
                currentSchema = mutation.Mutate(null, currentSchema, GetAttributeSchemaType());
                if (currentSchema == null)
                {
                    throw new EvitaInternalError("Attribute unexpectedly removed from inside!");
                }
            }

            Validate(currentSchema);
            UpdatedSchema = currentSchema;
            UpdatedSchemaDirty = false;
        }

        return UpdatedSchema;
    }
    
    protected abstract Type GetAttributeSchemaType();

    /// <summary>
    /// Method allows adding specific mutation on the fly.
    /// </summary>
    public abstract bool AddMutations(IAttributeSchemaMutation mutation);

    /// <summary>
    /// Returns collection of <see cref="IAttributeSchemaMutation"/> instances describing what changes occurred in the builder
    /// and which should be applied on the existing parent schema in particular version.
    /// Each mutation increases <see cref="IVersioned.Version"/> of the modified object and allows to detect race
    /// conditions based on "optimistic locking" mechanism in very granular way.
    /// </summary>
    /// <returns></returns>
    public abstract ICollection<IAttributeSchemaMutation> ToAttributeMutation();

    /// <summary>
    /// Method validates the consistency of an attribute schema.
    /// It basically checks the compatibility of the data type for filter/unique/sort index purposes.
    /// </summary>
    private static void Validate(TS currentSchema)
    {
        Type type = currentSchema.Type;
        Assert.IsTrue(
            !currentSchema.Sortable() || typeof(IComparable<>).IsAssignableFrom(type) || typeof(IComparable).IsAssignableFrom(type),
            "Data type `" + currentSchema.Type + "` in attribute schema `" + currentSchema.Name +
            "` must implement Comparable in order to be usable for indexing!"
        );
        Assert.IsTrue(
            !(currentSchema.Filterable() && currentSchema.Unique()),
            "Attribute `" + currentSchema.Name +
            "` cannot be both unique and filterable. Unique attributes are implicitly filterable!"
        );
        Assert.IsTrue(
            !(currentSchema.Sortable() && currentSchema.Type.IsArray),
            "Attribute `" + currentSchema.Name +
            "` is sortable but also an array. Arrays cannot be handled by sorting algorithm!"
        );
    }
}
