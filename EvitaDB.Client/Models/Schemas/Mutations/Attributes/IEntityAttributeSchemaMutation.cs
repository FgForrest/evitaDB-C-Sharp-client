using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public interface IEntityAttributeSchemaMutation : IAttributeSchemaMutation, IEntitySchemaMutation
{
    public IEntitySchema ReplaceAttributeIfDifferent(
        IEntitySchema entitySchema,
        IAttributeSchema existingAttributeSchema,
        IEntityAttributeSchema updatedAttributeSchema)
    {
        if (existingAttributeSchema.Equals(updatedAttributeSchema))
        {
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
            entitySchema.GetAttributes().Values
                .Where(x => updatedAttributeSchema.Name != x.Name)
                .Concat(new[] {updatedAttributeSchema})
                .ToDictionary(x => x.Name, x => x),
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}