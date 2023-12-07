﻿using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class ModifyAttributeSchemaTypeMutation : IEntityAttributeSchemaMutation,
    IGlobalAttributeSchemaMutation,
    IReferenceAttributeSchemaMutation, ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public Type Type { get; }
    public int IndexedDecimalPlaces { get; }

    public ModifyAttributeSchemaTypeMutation(string name, Type type, int indexedDecimalPlaces)
    {
        Name = name;
        Type = type;
        IndexedDecimalPlaces = indexedDecimalPlaces;
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
        IAttributeSchema updatedAttributeSchema = Mutate(null, existingAttributeSchema, typeof(IAttributeSchema));
        return (this as IReferenceAttributeSchemaMutation).ReplaceAttributeIfDifferent(
            referenceSchema, existingAttributeSchema, updatedAttributeSchema
        );
    }

    public TS Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema, Type schemaType) where TS : class, IAttributeSchema
    {
        Assert.IsPremiseValid(attributeSchema != null, "Attribute schema is mandatory!");
        if (attributeSchema is GlobalAttributeSchema globalAttributeSchema)
        {
            return (AttributeSchema.InternalBuild(
                Name,
                globalAttributeSchema.Description,
                globalAttributeSchema.DeprecationNotice,
                globalAttributeSchema.UniquenessType,
                globalAttributeSchema.GlobalUniquenessType,
                globalAttributeSchema.Filterable,
                globalAttributeSchema.Sortable,
                globalAttributeSchema.Localized,
                globalAttributeSchema.Nullable,
                globalAttributeSchema.Representative,
                Type,
                globalAttributeSchema.DefaultValue is not null ? EvitaDataTypes.ToTargetType(globalAttributeSchema.DefaultValue, Type) : null,
                IndexedDecimalPlaces
            ) as TS)!;
        }
        
        if (attributeSchema is EntityAttributeSchema entityAttributeSchema)
        {
            return (EntityAttributeSchema.InternalBuild(
                Name,
                entityAttributeSchema.Description,
                entityAttributeSchema.DeprecationNotice,
                entityAttributeSchema.UniquenessType,
                entityAttributeSchema.Filterable,
                entityAttributeSchema.Sortable,
                entityAttributeSchema.Localized,
                entityAttributeSchema.Nullable,
                entityAttributeSchema.Representative,
                Type,
                entityAttributeSchema.DefaultValue is not null ? EvitaDataTypes.ToTargetType(entityAttributeSchema.DefaultValue, Type) : null,
                IndexedDecimalPlaces
            ) as TS)!;
        }

        return (AttributeSchema.InternalBuild(
            Name,
            attributeSchema!.NameVariants,
            attributeSchema.Description,
            attributeSchema.DeprecationNotice,
            attributeSchema.UniquenessType,
            attributeSchema.Filterable,
            attributeSchema.Sortable,
            attributeSchema.Localized,
            attributeSchema.Nullable,
            Type,
            attributeSchema.DefaultValue is not null ? EvitaDataTypes.ToTargetType(attributeSchema.DefaultValue, Type) : null,
            IndexedDecimalPlaces
        ) as TS)!;
    }

    public IEntitySchema Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IEntityAttributeSchema existingAttributeSchema = entitySchema?.GetAttribute(Name) ??
                                                   throw new InvalidSchemaMutationException(
                                                       "The attribute `" + Name + "` is not defined in entity `" +
                                                       entitySchema?.Name + "` schema!"
                                                   );
        IEntityAttributeSchema updatedAttributeSchema = Mutate(catalogSchema, existingAttributeSchema, typeof(IEntityAttributeSchema));
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
        IGlobalAttributeSchema updatedAttributeSchema = Mutate(catalogSchema, existingAttributeSchema, typeof(IGlobalAttributeSchema));
        return (this as IGlobalAttributeSchemaMutation).ReplaceAttributeIfDifferent(
            catalogSchema, existingAttributeSchema, updatedAttributeSchema
        );
    }
}
