using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.References;
using EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Builders;

public class SortableAttributeCompoundSchemaBuilder : ISortableAttributeCompoundSchemaBuilder
{
    private ICatalogSchema CatalogSchema { get; }
    private IEntitySchema EntitySchema { get; }
    private IReferenceSchema? ReferenceSchema { get; }
    private ISortableAttributeCompoundSchema BaseSchema { get; }
    private IList<IEntitySchemaMutation> Mutations { get; } = new List<IEntitySchemaMutation>();
    private bool UpdatedSchemaDirty { get; set; }
    private ISortableAttributeCompoundSchema? UpdatedSchema { get; set; }
    
    private readonly ISortableAttributeCompoundSchema _instance;

    public string Name => _instance.Name;
    public string? Description => _instance.Description;
    public IDictionary<NamingConvention, string> NameVariants => _instance.NameVariants;
    public string? DeprecationNotice => _instance.DeprecationNotice;
    public IList<AttributeElement> AttributeElements => _instance.AttributeElements;

    internal SortableAttributeCompoundSchemaBuilder(
        ICatalogSchema catalogSchema,
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema,
        ISortableAttributeCompoundSchema? existingSchema,
        string name,
        IList<AttributeElement> attributeElements,
        IList<IEntitySchemaMutation> mutations,
        bool createNew
    )
    {
        CatalogSchema = catalogSchema;
        EntitySchema = entitySchema;
        ReferenceSchema = referenceSchema;
        BaseSchema = existingSchema ?? SortableAttributeCompoundSchema.InternalBuild(
            name, null, null, attributeElements
        );
        if (createNew)
        {
            Mutations.Add(
                new CreateSortableAttributeCompoundSchemaMutation(
                    BaseSchema.Name,
                    BaseSchema.Description,
                    BaseSchema.DeprecationNotice,
                    attributeElements.ToArray()
                )
            );
        }

        foreach (IEntitySchemaMutation mutation in mutations.Where(it =>
                     it is IReferenceSchemaMutation referenceSchemaMutation &&
                     name.Equals(referenceSchemaMutation.Name) &&
                     referenceSchemaMutation is not CreateReferenceSchemaMutation
                 )
                )
        {
            mutations.Add(mutation);
        }

        _instance ??= ToInstance();
    }

    public string GetNameVariant(NamingConvention namingConvention)
    {
        return _instance.GetNameVariant(namingConvention);
    }

    public ISortableAttributeCompoundSchemaBuilder WithDescription(string? description)
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new ModifySortableAttributeCompoundSchemaDescriptionMutation(Name, description)
        );
        return this;
    }

    public ISortableAttributeCompoundSchemaBuilder Deprecated(string deprecationNotice)
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation(Name, deprecationNotice)
        );
        return this;
    }

    public ISortableAttributeCompoundSchemaBuilder NotDeprecatedAnymore()
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation(Name, null)
        );
        return this;
    }

    public ICollection<IEntitySchemaMutation> ToMutation()
    {
        return Mutations;
    }

    public ICollection<ISortableAttributeCompoundSchemaMutation> ToSortableAttributeCompoundSchemaMutation()
    {
        return Mutations
            .Select(it => (ISortableAttributeCompoundSchemaMutation) it)
            .ToList();
    }

    public ICollection<IReferenceSchemaMutation> ToReferenceMutation(string referenceName)
    {
        return new List<IReferenceSchemaMutation>(Mutations
            .Select(it =>
                new ModifyReferenceSortableAttributeCompoundSchemaMutation(referenceName, (it as IReferenceSchemaMutation)!)));
    }

    public ISortableAttributeCompoundSchema ToInstance()
    {
        if (UpdatedSchema == null || UpdatedSchemaDirty)
        {
            ISortableAttributeCompoundSchema? currentSchema = BaseSchema;
            foreach (IEntitySchemaMutation mutation in Mutations)
            {
                currentSchema =
                    ((ISortableAttributeCompoundSchemaMutation) mutation).Mutate(EntitySchema, ReferenceSchema,
                        currentSchema);
                if (currentSchema == null)
                {
                    throw new EvitaInternalError("Attribute unexpectedly removed from inside!");
                }
            }

            UpdatedSchema = currentSchema;
            UpdatedSchemaDirty = false;
        }

        return UpdatedSchema;
    }
}