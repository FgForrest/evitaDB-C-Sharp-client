using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class ModifyAttributeSchemaDefaultValueMutation : IEntityAttributeSchemaMutation, IGlobalAttributeSchemaMutation,
    IReferenceAttributeSchemaMutation, ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public object? DefaultValue { get; }

    public ModifyAttributeSchemaDefaultValueMutation(string name, object? defaultValue)
    {
        Name = name;
        DefaultValue = defaultValue;
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        IAttributeSchema existingAttributeSchema = referenceSchema!.GetAttribute(Name) ?? throw new InvalidSchemaMutationException(
                "The attribute `" + Name + "` is not defined in entity `" + entitySchema.Name +
                "` schema for reference with name `" + referenceSchema.Name + "`!"
            );

        try {
            AttributeSchema updatedAttributeSchema = AttributeSchema.InternalBuild(
                Name,
                existingAttributeSchema.Description,
                existingAttributeSchema.DeprecationNotice,
                existingAttributeSchema.UniquenessType,
                existingAttributeSchema.Filterable,
                existingAttributeSchema.Sortable,
                existingAttributeSchema.Localized,
                existingAttributeSchema.Nullable,
                existingAttributeSchema.Type,
                EvitaDataTypes.ToTargetType(DefaultValue, existingAttributeSchema.Type),
                existingAttributeSchema.IndexedDecimalPlaces
            );
            return (this as IReferenceAttributeSchemaMutation).ReplaceAttributeIfDifferent(
                referenceSchema, existingAttributeSchema, updatedAttributeSchema
            );
        } catch (UnsupportedDataTypeException) {
            throw new InvalidSchemaMutationException(
                "The value `" + DefaultValue + "` cannot be automatically converted to " +
                "attribute `" + Name + "` type `" + existingAttributeSchema.Type +
                "` in entity `" + entitySchema.Name + "` schema!"
            );
        }
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
                globalAttributeSchema.Type,
                EvitaDataTypes.ToTargetType(DefaultValue, globalAttributeSchema.Type),
                globalAttributeSchema.IndexedDecimalPlaces
            ) as TS)!;
        }
        
        if (attributeSchema is EntityAttributeSchema entityAttributeSchema)
        {
            return (EntityAttributeSchema.InternalBuild(
                Name,
                entityAttributeSchema.NameVariants,
                entityAttributeSchema.Description,
                entityAttributeSchema.DeprecationNotice,
                entityAttributeSchema.UniquenessType,
                entityAttributeSchema.Filterable,
                entityAttributeSchema.Sortable,
                entityAttributeSchema.Localized,
                entityAttributeSchema.Nullable,
                entityAttributeSchema.Representative,
                entityAttributeSchema.Type,
                EvitaDataTypes.ToTargetType(DefaultValue, entityAttributeSchema.Type),
                entityAttributeSchema.IndexedDecimalPlaces
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
            attributeSchema.Type,
            EvitaDataTypes.ToTargetType(DefaultValue, attributeSchema.Type),
            attributeSchema.IndexedDecimalPlaces
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
        try
        {
            EntityAttributeSchema updatedAttributeSchema = EntityAttributeSchema.InternalBuild(
                Name,
                existingAttributeSchema.NameVariants,
                existingAttributeSchema.Description,
                existingAttributeSchema.DeprecationNotice,
                existingAttributeSchema.UniquenessType,
                existingAttributeSchema.Filterable,
                existingAttributeSchema.Sortable,
                existingAttributeSchema.Localized,
                existingAttributeSchema.Nullable,
                existingAttributeSchema.Representative,
                existingAttributeSchema.Type,
                EvitaDataTypes.ToTargetType(DefaultValue, existingAttributeSchema.Type),
                existingAttributeSchema.IndexedDecimalPlaces
            );
            return (this as IEntityAttributeSchemaMutation).ReplaceAttributeIfDifferent(
                entitySchema, existingAttributeSchema, updatedAttributeSchema
            );
        }
        catch (UnsupportedDataTypeException)
        {
            throw new InvalidSchemaMutationException(
                "The value `" + DefaultValue + "` cannot be automatically converted to " +
                "attribute `" + Name + "` type `" + existingAttributeSchema.Type +
                "` in entity `" + entitySchema.Name + "` schema!"
            );
        }
    }

    public ICatalogSchema Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        IGlobalAttributeSchema existingAttributeSchema = catalogSchema?.GetAttribute(Name) ??
                                                         throw new InvalidSchemaMutationException("The attribute `" +
                                                             Name + "` is not defined in catalog `" +
                                                             catalogSchema?.Name + "` schema!");
        try
        {
            IGlobalAttributeSchema updatedAttributeSchema = Mutate(catalogSchema, existingAttributeSchema, typeof(IGlobalAttributeSchema));
            return (this as IGlobalAttributeSchemaMutation).ReplaceAttributeIfDifferent(
                catalogSchema, existingAttributeSchema, updatedAttributeSchema
            );
        }
        catch (UnsupportedDataTypeException)
        {
            throw new InvalidSchemaMutationException(
                "The value `" + DefaultValue + "` cannot be automatically converted to " +
                "attribute `" + Name + "` type `" + existingAttributeSchema.Type +
                "` in catalog `" + catalogSchema.Name + "`!"
            );
        }
    }
}
