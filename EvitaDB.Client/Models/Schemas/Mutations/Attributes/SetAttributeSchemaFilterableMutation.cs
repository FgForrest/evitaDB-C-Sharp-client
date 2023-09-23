﻿using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class SetAttributeSchemaFilterableMutation : IEntityAttributeSchemaMutation,
    IGlobalAttributeSchemaMutation,
    IReferenceAttributeSchemaMutation, ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public bool Filterable { get; }

    public SetAttributeSchemaFilterableMutation(string name, bool filterable)
    {
        Name = name;
        Filterable = filterable;
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        Assert.IsTrue(
            referenceSchema!.IsIndexed,
            () => new InvalidSchemaMutationException(
                "The reference `" + referenceSchema.Name + "` is in entity `" + entitySchema.Name + "` is not indexed! " +
                "Non-indexed references must not contain filterable attribute `" + Name + "`!"
            )
            );
        IAttributeSchema existingAttributeSchema = referenceSchema.GetAttribute(Name) ??
                                                   throw new InvalidSchemaMutationException(
                                                       "The attribute `" + Name + "` is not defined in entity `" +
                                                       entitySchema.Name +
                                                       "` schema for reference with name `" + referenceSchema.Name +
                                                       "`!"
                                                   );
        IAttributeSchema updatedAttributeSchema = Mutate(null, existingAttributeSchema);
        return (this as IReferenceAttributeSchemaMutation).ReplaceAttributeIfDifferent(
            referenceSchema, existingAttributeSchema, updatedAttributeSchema
        );
    }

    public TS Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema) where TS : IAttributeSchema
    {
        Assert.IsPremiseValid(attributeSchema != null, "Attribute schema is mandatory!");
        if (attributeSchema is GlobalAttributeSchema globalAttributeSchema)
        {
            return (TS) Convert.ChangeType(AttributeSchema.InternalBuild(
                Name,
                globalAttributeSchema.Description,
                globalAttributeSchema.DeprecationNotice,
                globalAttributeSchema.Unique,
                globalAttributeSchema.UniqueGlobally,
                Filterable,
                globalAttributeSchema.Sortable,
                globalAttributeSchema.Localized,
                globalAttributeSchema.Nullable,
                globalAttributeSchema.GetType(),
                globalAttributeSchema.DefaultValue,
                globalAttributeSchema.IndexedDecimalPlaces
            ), typeof(TS));
        }

        return (TS) Convert.ChangeType(AttributeSchema.InternalBuild(
            Name,
            attributeSchema!.NameVariants,
            attributeSchema.Description,
            attributeSchema.DeprecationNotice,
            attributeSchema.Unique,
            Filterable,
            attributeSchema.Sortable,
            attributeSchema.Localized,
            attributeSchema.Nullable,
            attributeSchema.GetType(),
            attributeSchema.DefaultValue,
            attributeSchema.IndexedDecimalPlaces
        ), typeof(TS));
    }

    public IEntitySchema Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IAttributeSchema existingAttributeSchema = entitySchema?.GetAttribute(Name) ??
                                                   throw new InvalidSchemaMutationException(
                                                       "The attribute `" + Name + "` is not defined in entity `" +
                                                       entitySchema?.Name + "` schema!"
                                                   );
        IAttributeSchema updatedAttributeSchema = Mutate(catalogSchema, existingAttributeSchema);
        return (this as IEntityAttributeSchemaMutation).ReplaceAttributeIfDifferent(
            entitySchema, existingAttributeSchema, updatedAttributeSchema
        );
    }

    public ICatalogSchema Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        IGlobalAttributeSchema existingAttributeSchema = catalogSchema?.GetAttribute(Name) ??
                                                         throw new InvalidSchemaMutationException("The attribute `" +
                                                             Name + "` is not defined in catalog `" +
                                                             catalogSchema?.Name + "` schema!");
        IGlobalAttributeSchema updatedAttributeSchema = Mutate(catalogSchema, existingAttributeSchema);
        return (this as IGlobalAttributeSchemaMutation).ReplaceAttributeIfDifferent(
            catalogSchema, existingAttributeSchema, updatedAttributeSchema
        );
    }
}