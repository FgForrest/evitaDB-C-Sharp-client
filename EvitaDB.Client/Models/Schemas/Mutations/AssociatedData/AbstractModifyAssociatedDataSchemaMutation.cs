using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

public abstract class AbstractModifyAssociatedDataSchemaMutation : IEntitySchemaMutation, IAssociatedDataSchemaMutation
{
    public string Name { get; }

    protected AbstractModifyAssociatedDataSchemaMutation(string name)
    {
        Name = name;
    }

    protected IEntitySchema ReplaceAssociatedDataIfDifferent(
        IEntitySchema entitySchema,
        IAssociatedDataSchema existingAssociatedDataSchema,
        IAssociatedDataSchema updatedAssociatedDataSchema
    )
    {
        if (existingAssociatedDataSchema.Equals(updatedAssociatedDataSchema))
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
            entitySchema.WithGeneratedPrimaryKey,
            entitySchema.WithHierarchy,
            entitySchema.WithPrice,
            entitySchema.IndexedPricePlaces,
            entitySchema.Locales,
            entitySchema.Currencies,
            entitySchema.Attributes,
            entitySchema.AssociatedData.Values.Where(it => updatedAssociatedDataSchema.Name != it.Name)
                .Concat(new[] {updatedAssociatedDataSchema})
                .ToDictionary(x => x.Name, x => x),
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }

    public abstract IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema);
    public abstract IAssociatedDataSchema Mutate(IAssociatedDataSchema? associatedDataSchema);
}