﻿using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Entities;

public class DisallowEvolutionModeInEntitySchemaMutation : IEntitySchemaMutation
{
    public ISet<EvolutionMode> EvolutionModes { get; }

    public DisallowEvolutionModeInEntitySchemaMutation(params EvolutionMode[] evolutionModes)
    {
        EvolutionModes = new HashSet<EvolutionMode>(evolutionModes);
    }

    public DisallowEvolutionModeInEntitySchemaMutation(ISet<EvolutionMode> evolutionModes)
    {
        EvolutionModes = new HashSet<EvolutionMode>(evolutionModes);
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        if (entitySchema!.EvolutionModes.All(EvolutionModes.Contains))
        {
            // no need to change the schema
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
            entitySchema.EvolutionModes.Where(x => !EvolutionModes.Contains(x)).ToHashSet(),
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}