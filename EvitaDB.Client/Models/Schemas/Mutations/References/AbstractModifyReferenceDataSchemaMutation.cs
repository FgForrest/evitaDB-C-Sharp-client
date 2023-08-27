using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations.References;

public abstract class AbstractModifyReferenceDataSchemaMutation : IEntitySchemaMutation, IReferenceSchemaMutation
{
    public string Name { get; }

    protected AbstractModifyReferenceDataSchemaMutation(string name)
    {
        Name = name;
    }

    public abstract IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema);
    public abstract IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema);

    protected IEntitySchema ReplaceReferenceSchema(
        IEntitySchema entitySchema,
        IReferenceSchema existingReferenceSchema,
        IReferenceSchema updatedReferenceSchema
    )
    {
        if (existingReferenceSchema.Equals(updatedReferenceSchema))
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
            entitySchema.AssociatedData,
            entitySchema.References.Values.Where(it => existingReferenceSchema.Name != it.Name)
                .Concat(new[] {updatedReferenceSchema})
                .ToDictionary(x => x.Name, x => x),
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}