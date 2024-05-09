using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ISortableAttributeCompoundSchemaMutation : ISchemaMutation
{
    string Name { get; }

    SortableAttributeCompoundSchema? Mutate(
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema,
        ISortableAttributeCompoundSchema? sortableAttributeCompoundSchema
    );

    IEntitySchema ReplaceSortableAttributeCompoundIfDifferent(
        IEntitySchema entitySchema,
        ISortableAttributeCompoundSchema existingSchema,
        SortableAttributeCompoundSchema updatedSchema
    )
    {
        if (existingSchema.Equals(updatedSchema))
        {
            // we don't need to update entity schema - the associated data already contains the requested change
            return entitySchema;
        }

        return EntitySchema.InternalBuild(
            entitySchema.Version + 1,
            entitySchema.Name,
            entitySchema.NameVariants,
            entitySchema.Description,
            entitySchema.DeprecationNotice,
            entitySchema.WithGeneratedPrimaryKey(),
            entitySchema.WithHierarchy(),
            entitySchema.WithPrice(),
            entitySchema.IndexedPricePlaces,
            entitySchema.Locales,
            entitySchema.Currencies,
            entitySchema.Attributes,
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds().Values
                .Where(it => updatedSchema.Name != it.Name).Concat(new[] {updatedSchema})
                .ToDictionary(x => x.Name, x => x)
        );
    }
}
