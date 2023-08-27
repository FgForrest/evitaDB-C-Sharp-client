using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Entities;

public class SetEntitySchemaWithPriceMutation : IEntitySchemaMutation
{
    public bool WithPrice { get; }
    public int IndexedPricePlaces { get; }

    public SetEntitySchemaWithPriceMutation(bool withPrice, int indexedPricePlaces)
    {
        WithPrice = withPrice;
        IndexedPricePlaces = indexedPricePlaces;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        if (WithPrice == entitySchema!.WithHierarchy)
        {
            // entity schema is already removed - no need to do anything
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
            WithPrice,
            IndexedPricePlaces,
            entitySchema.Locales,
            entitySchema.Currencies,
            entitySchema.Attributes,
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}