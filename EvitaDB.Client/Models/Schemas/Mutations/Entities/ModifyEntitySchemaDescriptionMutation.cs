using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Entities;

public class ModifyEntitySchemaDescriptionMutation : IEntitySchemaMutation
{
    public string? Description { get; }

    public ModifyEntitySchemaDescriptionMutation(string? description)
    {
        Description = description;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        if (Equals(entitySchema!.Description, Description))
        {
            // entity schema is already removed - no need to do anything
            return entitySchema;
        }

        return EntitySchema.InternalBuild(
            entitySchema.Version + 1,
            entitySchema.Name,
            entitySchema.NameVariants,
            Description,
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
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}
