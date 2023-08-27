using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.SortableAttributeCompounds;

public class RemoveSortableAttributeCompoundSchemaMutation : IEntitySchemaMutation, IReferenceSortableAttributeCompoundSchemaMutation
{
    public string Name { get; }

    public RemoveSortableAttributeCompoundSchemaMutation(string name)
    {
        Name = name;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        SortableAttributeCompoundSchema? existingAttributeSchema = entitySchema!.GetSortableAttributeCompound(Name);
        if (existingAttributeSchema is null) {
            // the sortable attribute compound schema was already removed - or just doesn't exist,
            // so we can simply return current schema
            return entitySchema;
        }

        return EntitySchema.InternalBuild(
            entitySchema.Version + 1,
            entitySchema.Name,
            entitySchema.NameVariants,
            entitySchema.Description,
            entitySchema.DeprecationNotice,
            entitySchema.WithGeneratedPrimaryKey,
            entitySchema.WithHierarchy,
            entitySchema.WithPrice,
            entitySchema.IndexedPricePlaces,
            entitySchema.Locales,
            entitySchema.Currencies,
            entitySchema.Attributes,
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
                .Where(it => !Equals(Name, it.Key))
                .ToDictionary(x=>x.Key, x=>x.Value)
        );
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        SortableAttributeCompoundSchema? existingAttributeSchema = referenceSchema!.GetSortableAttributeCompound(Name);
        if (existingAttributeSchema is null) {
            // the attribute schema was already removed - or just doesn't exist,
            // so we can simply return current schema
            return referenceSchema;
        }

        return ReferenceSchema.InternalBuild(
            referenceSchema.Name,
            referenceSchema.NameVariants,
            referenceSchema.Description,
            referenceSchema.DeprecationNotice,
            referenceSchema.ReferencedEntityType,
            referenceSchema.GetEntityTypeNameVariants(_ => null),
            referenceSchema.ReferencedEntityTypeManaged,
            referenceSchema.Cardinality,
            referenceSchema.ReferencedGroupType,
            referenceSchema.GetGroupTypeNameVariants(_ => null),
            referenceSchema.ReferencedGroupTypeManaged,
            referenceSchema.Indexed,
            referenceSchema.Faceted,
            referenceSchema.GetAttributes(),
            referenceSchema.GetSortableAttributeCompounds()
                .Where(it => !Equals(Name, it.Key))
                .ToDictionary(x=>x.Key, x=>x.Value)
        );
    }

    public SortableAttributeCompoundSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema,
        ISortableAttributeCompoundSchema? sortableAttributeCompoundSchema)
    {
        Assert.IsPremiseValid(sortableAttributeCompoundSchema != null, "Sortable attribute compound schema is mandatory!");
        return null;
    }
}