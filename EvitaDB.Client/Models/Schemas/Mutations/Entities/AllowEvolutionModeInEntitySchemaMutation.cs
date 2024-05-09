using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Entities;

public class AllowEvolutionModeInEntitySchemaMutation : IEntitySchemaMutation
{
    public EvolutionMode[] EvolutionModes { get; }
    
    public AllowEvolutionModeInEntitySchemaMutation(params EvolutionMode[] evolutionModes)
    {
        EvolutionModes = evolutionModes;
    }


    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        if (entitySchema!.EvolutionModes.All(EvolutionModes.Contains)) {
            // no need to change the schema
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
            entitySchema.EvolutionModes.Concat(EvolutionModes).ToHashSet(),
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}
