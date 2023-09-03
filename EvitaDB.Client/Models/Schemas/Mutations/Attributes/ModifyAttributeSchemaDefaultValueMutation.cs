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
        IAttributeSchema existingAttributeSchema = referenceSchema.GetAttribute(Name) ?? throw new InvalidSchemaMutationException(
                "The attribute `" + Name + "` is not defined in entity `" + entitySchema.Name +
                "` schema for reference with name `" + referenceSchema.Name + "`!"
            );

        try {
            AttributeSchema updatedAttributeSchema = AttributeSchema.InternalBuild(
                Name,
                existingAttributeSchema.Description,
                existingAttributeSchema.DeprecationNotice,
                existingAttributeSchema.Unique,
                existingAttributeSchema.Filterable,
                existingAttributeSchema.Sortable,
                existingAttributeSchema.Localized,
                existingAttributeSchema.Nullable,
                existingAttributeSchema.GetType(),
                EvitaDataTypes.ToTargetType(DefaultValue, existingAttributeSchema.GetType()),
                existingAttributeSchema.IndexedDecimalPlaces
            );
            return (this as IReferenceAttributeSchemaMutation).ReplaceAttributeIfDifferent(
                referenceSchema, existingAttributeSchema, updatedAttributeSchema
            );
        } catch (UnsupportedDataTypeException) {
            throw new InvalidSchemaMutationException(
                "The value `" + DefaultValue + "` cannot be automatically converted to " +
                "attribute `" + Name + "` type `" + existingAttributeSchema.GetType() +
                "` in entity `" + entitySchema.Name + "` schema!"
            );
        }
    }

    public TS? Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema) where TS : IAttributeSchema
    {
        Assert.IsPremiseValid(attributeSchema != null, "Attribute schema is mandatory!");
        if (attributeSchema is GlobalAttributeSchema globalAttributeSchema)
        {
            return (TS) Convert.ChangeType(GlobalAttributeSchema.InternalBuild(
                Name,
                globalAttributeSchema.Description,
                globalAttributeSchema.DeprecationNotice,
                globalAttributeSchema.Unique,
                globalAttributeSchema.UniqueGlobally,
                globalAttributeSchema.Filterable,
                globalAttributeSchema.Sortable,
                globalAttributeSchema.Localized,
                globalAttributeSchema.Nullable,
                globalAttributeSchema.GetType(),
                EvitaDataTypes.ToTargetType(DefaultValue, globalAttributeSchema.GetType()),
                globalAttributeSchema.IndexedDecimalPlaces
            ), typeof(TS));
        }

        return (TS) Convert.ChangeType(AttributeSchema.InternalBuild(
            Name,
            attributeSchema.NameVariants,
            attributeSchema.Description,
            attributeSchema.DeprecationNotice,
            attributeSchema.Unique,
            attributeSchema.Filterable,
            attributeSchema.Sortable,
            attributeSchema.Localized,
            attributeSchema.Nullable,
            attributeSchema.GetType(),
            EvitaDataTypes.ToTargetType(DefaultValue, attributeSchema.GetType()),
            attributeSchema.IndexedDecimalPlaces
        ), typeof(TS));
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IAttributeSchema existingAttributeSchema = entitySchema?.GetAttribute(Name) ??
                                                   throw new InvalidSchemaMutationException(
                                                       "The attribute `" + Name + "` is not defined in entity `" +
                                                       entitySchema?.Name + "` schema!"
                                                   );
        try
        {
            AttributeSchema updatedAttributeSchema = AttributeSchema.InternalBuild(
                Name,
                existingAttributeSchema.NameVariants,
                existingAttributeSchema.Description,
                existingAttributeSchema.DeprecationNotice,
                existingAttributeSchema.Unique,
                existingAttributeSchema.Filterable,
                existingAttributeSchema.Sortable,
                existingAttributeSchema.Localized,
                existingAttributeSchema.Nullable,
                existingAttributeSchema.GetType(),
                EvitaDataTypes.ToTargetType(DefaultValue, existingAttributeSchema.GetType()),
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
                "attribute `" + Name + "` type `" + existingAttributeSchema.GetType() +
                "` in entity `" + entitySchema.Name + "` schema!"
            );
        }
    }

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        IGlobalAttributeSchema existingAttributeSchema = catalogSchema?.GetAttribute(Name) ??
                                                         throw new InvalidSchemaMutationException("The attribute `" +
                                                             Name + "` is not defined in catalog `" +
                                                             catalogSchema?.Name + "` schema!");
        try
        {
            IGlobalAttributeSchema? updatedAttributeSchema = Mutate(catalogSchema, existingAttributeSchema);
            return (this as IGlobalAttributeSchemaMutation).ReplaceAttributeIfDifferent(
                catalogSchema, existingAttributeSchema, updatedAttributeSchema!
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