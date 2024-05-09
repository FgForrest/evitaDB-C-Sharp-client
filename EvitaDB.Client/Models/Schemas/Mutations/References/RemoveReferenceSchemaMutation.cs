using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.References;

public class RemoveReferenceSchemaMutation : IReferenceSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }

    public RemoveReferenceSchemaMutation(string name)
    {
        Name = name;
    }

    public IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        return null;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IReferenceSchema? existingReferenceSchema = entitySchema!.GetReference(Name);
        if (existingReferenceSchema is null)
        {
            // the reference schema was already removed - or just doesn't exist,
            // so we can simply return current schema
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
            entitySchema.References.Values
                .Where(it => it.Name != Name)
                .ToDictionary(x => x.Name, x => x),
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}
