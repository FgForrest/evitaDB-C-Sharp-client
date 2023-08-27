using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Entities;

public class SetEntitySchemaWithGeneratedPrimaryKeyMutation : IEntitySchemaMutation
{
    public bool WithGeneratedPrimaryKey { get; }

    public SetEntitySchemaWithGeneratedPrimaryKeyMutation(bool withGeneratedPrimaryKey)
    {
        WithGeneratedPrimaryKey = withGeneratedPrimaryKey;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        if (WithGeneratedPrimaryKey == entitySchema!.WithGeneratedPrimaryKey)
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
            WithGeneratedPrimaryKey,
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
        );
    }
}