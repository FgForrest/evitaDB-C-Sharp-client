using System.Globalization;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Entities;

public class AllowLocaleInEntitySchemaMutation : IEntitySchemaMutation
{
    public CultureInfo[] Locales { get; }
    
    public AllowLocaleInEntitySchemaMutation(params CultureInfo[] locales)
    {
        Locales = locales;
    }


    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        if (entitySchema!.Locales.All(Locales.Contains)) {
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
            entitySchema.Locales.Concat(Locales).ToHashSet(),
            entitySchema.Currencies,
            entitySchema.Attributes,
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}
