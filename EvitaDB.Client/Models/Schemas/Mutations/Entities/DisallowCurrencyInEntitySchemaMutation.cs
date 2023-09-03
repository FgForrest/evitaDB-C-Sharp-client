using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Entities;

public class DisallowCurrencyInEntitySchemaMutation : IEntitySchemaMutation
{
    public ISet<Currency> Currencies { get; }

    public DisallowCurrencyInEntitySchemaMutation(params Currency[] currencies)
    {
        Currencies = new HashSet<Currency>(currencies);
    }

    public DisallowCurrencyInEntitySchemaMutation(ISet<Currency> currencies)
    {
        Currencies = new HashSet<Currency>(currencies);
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        if (!Currencies.Any(entitySchema!.SupportsCurrency))
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
            entitySchema.Currencies.Where(x => !Currencies.Contains(x)).ToHashSet(),
            entitySchema.Attributes,
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}