﻿using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class ModifyAttributeSchemaNameMutation : IEntityAttributeSchemaMutation,
    IGlobalAttributeSchemaMutation,
    IReferenceAttributeSchemaMutation, ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public string NewName { get; }

    public ModifyAttributeSchemaNameMutation(string name, string newName)
    {
        Name = name;
        NewName = newName;
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        IAttributeSchema existingAttributeSchema = referenceSchema!.GetAttribute(Name) ??
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

    public TS Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema) where TS : class, IAttributeSchema
    {
        Assert.IsPremiseValid(attributeSchema != null, "Attribute schema is mandatory!");
        if (attributeSchema is GlobalAttributeSchema globalAttributeSchema)
        {
            return (AttributeSchema.InternalBuild(
                NewName,
                globalAttributeSchema.Description,
                globalAttributeSchema.DeprecationNotice,
                globalAttributeSchema.Unique,
                globalAttributeSchema.UniqueGlobally,
                globalAttributeSchema.Filterable,
                globalAttributeSchema.Sortable,
                globalAttributeSchema.Localized,
                globalAttributeSchema.Nullable,
                globalAttributeSchema.Type,
                globalAttributeSchema.DefaultValue,
                globalAttributeSchema.IndexedDecimalPlaces
            ) as TS)!;
        }

        return (AttributeSchema.InternalBuild(
            NewName,
            attributeSchema!.NameVariants,
            attributeSchema.Description,
            attributeSchema.DeprecationNotice,
            attributeSchema.Unique,
            attributeSchema.Filterable,
            attributeSchema.Sortable,
            attributeSchema.Localized,
            attributeSchema.Nullable,
            attributeSchema.Type,
            attributeSchema.DefaultValue,
            attributeSchema.IndexedDecimalPlaces
        ) as TS)!;
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