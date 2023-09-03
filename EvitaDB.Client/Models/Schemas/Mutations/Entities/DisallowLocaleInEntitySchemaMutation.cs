using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Entities;

public class DisallowLocaleInEntitySchemaMutation : IEntitySchemaMutation
{
    public ISet<CultureInfo> Locales { get; }

    public DisallowLocaleInEntitySchemaMutation(params CultureInfo[] locales)
    {
        Locales = new HashSet<CultureInfo>(locales);
    }

    public DisallowLocaleInEntitySchemaMutation(ISet<CultureInfo> locales)
    {
        Locales = new HashSet<CultureInfo>(locales);
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        if (entitySchema!.Locales.All(Locales.Contains))
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
            entitySchema.Locales.Where(x => !Locales.Contains(x)).ToHashSet(),
            entitySchema.Currencies,
            entitySchema.Attributes,
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}