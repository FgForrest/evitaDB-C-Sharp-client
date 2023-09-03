using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class RemoveAttributeSchemaMutation : IGlobalAttributeSchemaMutation, IReferenceAttributeSchemaMutation,
    ILocalCatalogSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }

    public RemoveAttributeSchemaMutation(string name)
    {
        Name = name;
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        IAttributeSchema? existingAttributeSchema = referenceSchema!.GetAttribute(Name);
        if (existingAttributeSchema is null)
        {
            // the attribute schema was already removed - or just doesn't exist,
            // so we can simply return current schema
            return referenceSchema;
        }

        return ReferenceSchema.InternalBuild(
            referenceSchema.Name,
            referenceSchema.NameVariants,
            referenceSchema.Description,
            referenceSchema.DeprecationNotice,
            referenceSchema.ReferencedEntityType,
            referenceSchema.GetEntityTypeNameVariants(_ => null),
            referenceSchema.ReferencedEntityTypeManaged,
            referenceSchema.Cardinality,
            referenceSchema.ReferencedGroupType,
            referenceSchema.GetGroupTypeNameVariants(_ => null),
            referenceSchema.ReferencedGroupTypeManaged,
            referenceSchema.Indexed,
            referenceSchema.Faceted,
            referenceSchema.GetAttributes().Values
                .Where(it => it.Name != Name)
                .ToDictionary(x => x.Name, x => x),
            referenceSchema.GetSortableAttributeCompounds()
        );
    }

    public TS? Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema) where TS : IAttributeSchema
    {
        Assert.IsPremiseValid(attributeSchema != null, "Attribute schema is mandatory!");
        return default;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IAttributeSchema? existingAttributeSchema = entitySchema!.GetAttribute(Name);
        if (existingAttributeSchema is null)
        {
            // the attribute schema was already removed - or just doesn't exist,
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
            entitySchema.GetAttributes().Values
                .Where(it => it.Name != Name)
                .ToDictionary(x => x.Name, x => x),
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        IGlobalAttributeSchema? existingAttributeSchema = catalogSchema!.GetAttribute(Name);
        if (existingAttributeSchema is null)
        {
            // the attribute schema was already removed - or just doesn't exist,
            // so we can simply return current schema
            return catalogSchema;
        }

        return CatalogSchema.InternalBuild(
            catalogSchema.Version + 1,
            catalogSchema.Name,
            catalogSchema.NameVariants,
            catalogSchema.Description,
            catalogSchema.CatalogEvolutionModes,
            catalogSchema.GetAttributes().Values
                .Where(it => it.Name != Name)
                .ToDictionary(x => x.Name, x => x),
            catalogSchema is CatalogSchema cs
                ? cs.EntitySchemaAccessor
                : _ => throw new NotSupportedException(
                    "Mutated schema is not able to provide access to entity schemas!"
                ));
    }
}