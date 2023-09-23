using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

public class RemoveAssociatedDataSchemaMutation : IAssociatedDataSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }

    public RemoveAssociatedDataSchemaMutation(string name)
    {
        Name = name;
    }

    public IAssociatedDataSchema? Mutate(IAssociatedDataSchema? associatedDataSchema)
    {
        Assert.IsPremiseValid(associatedDataSchema != null, "Associated data schema is mandatory!");
        return null;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IAssociatedDataSchema? existingAssociatedDataSchema = entitySchema.GetAssociatedData(Name);
        if (existingAssociatedDataSchema is null) {
            // the associated data schema was already removed - or just doesn't exist,
            // so we can simply return current schema
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
            entitySchema.AssociatedData.Values
                .Where(it => it.Name != Name)
                .ToDictionary(x => x.Name, x => x),
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}