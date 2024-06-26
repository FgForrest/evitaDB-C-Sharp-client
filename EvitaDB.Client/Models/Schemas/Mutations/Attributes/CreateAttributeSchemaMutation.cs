﻿using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class CreateAttributeSchemaMutation : IAttributeSchemaMutation, IReferenceSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public AttributeUniquenessType Unique { get; }
    public bool Filterable { get; }
    public bool Sortable { get; }
    public bool Localized { get; }
    public bool Nullable { get; }
    public bool Representative { get; }
    public Type Type { get; }
    public object? DefaultValue { get; }
    public int IndexedDecimalPlaces { get; }

    public CreateAttributeSchemaMutation(string name, string? description, string? deprecationNotice, AttributeUniquenessType? unique,
        bool filterable, bool sortable, bool localized, bool nullable, bool representative, Type type,
        object? defaultValue, int indexedDecimalPlaces)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Attribute, name);
        Name = name;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Unique = unique ?? AttributeUniquenessType.NotUnique;
        Filterable = filterable;
        Sortable = sortable;
        Localized = localized;
        Nullable = nullable;
        Representative = representative;
        Type = type;
        DefaultValue = defaultValue;
        IndexedDecimalPlaces = indexedDecimalPlaces;
    }

    public TS Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema, Type schemaType) where TS : class, IAttributeSchema
    {
        if (schemaType == typeof(IGlobalAttributeSchema))
        {
            return (AttributeSchema.InternalBuild(
                Name, Description, DeprecationNotice, Unique, null, Filterable, Sortable, Localized, 
                Nullable, Representative, Type, DefaultValue, IndexedDecimalPlaces
            ) as TS)!;
        }
        
        if (schemaType == typeof(IEntityAttributeSchema))
        {
            return (EntityAttributeSchema.InternalBuild(
                Name, Description, DeprecationNotice, Unique, Filterable, Sortable, Localized, 
                Nullable, Representative, Type, DefaultValue, IndexedDecimalPlaces
            ) as TS)!;
        }
        
        if (schemaType == typeof(IAttributeSchema))
        {
            return (AttributeSchema.InternalBuild(
                Name, Description, DeprecationNotice, Unique, Filterable, Sortable, Localized, 
                Nullable, Type, DefaultValue, IndexedDecimalPlaces
            ) as TS)!;
        }
        
        throw new InvalidSchemaMutationException("Unsupported schema type: " + schemaType);
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        AttributeSchema newAttributeSchema = AttributeSchema.InternalBuild(
            Name, Description, DeprecationNotice,
            Unique, Filterable, Sortable, Localized, Nullable,
            Type, DefaultValue,
            IndexedDecimalPlaces
        );
        IAttributeSchema? existingAttributeSchema = referenceSchema!.GetAttribute(Name);
        if (existingAttributeSchema == null)
        {
            return ReferenceSchema.InternalBuild(
                referenceSchema.Name,
                referenceSchema.NameVariants,
                referenceSchema.Description,
                referenceSchema.DeprecationNotice,
                referenceSchema.ReferencedEntityType,
                referenceSchema.ReferencedEntityTypeManaged
                    ? new Dictionary<NamingConvention, string?>()
                    : referenceSchema.GetEntityTypeNameVariants(_ => null!),
                referenceSchema.ReferencedEntityTypeManaged,
                referenceSchema.Cardinality,
                referenceSchema.ReferencedGroupType,
                referenceSchema.ReferencedGroupTypeManaged
                    ? new Dictionary<NamingConvention, string?>()
                    : referenceSchema.GetGroupTypeNameVariants(_ => null!),
                referenceSchema.ReferencedGroupTypeManaged,
                referenceSchema.IsIndexed,
                referenceSchema.IsFaceted,
                referenceSchema.GetAttributes().Values.Concat(new[] {newAttributeSchema})
                    .ToDictionary(x => x.Name, x => x),
                referenceSchema.GetSortableAttributeCompounds()
            );
        }

        if (existingAttributeSchema.Equals(newAttributeSchema))
        {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return referenceSchema;
        }

        // ups, there is conflict in attribute settings
        throw new InvalidSchemaMutationException(
            "The attribute `" + Name + "` already exists in entity `" + entitySchema.Name + "`" +
            " reference `" + referenceSchema.Name + "` schema and" +
            " it has different definition. To alter existing attribute schema you need to use different mutations."
        );
    }

    public IEntitySchema Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IEntityAttributeSchema newAttributeSchema = Mutate(catalogSchema, (IEntityAttributeSchema?) null, typeof(IEntityAttributeSchema));
        IAttributeSchema? existingAttributeSchema = entitySchema!.GetAttribute(Name);
        if (existingAttributeSchema == null)
        {
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
                entitySchema.GetAttributes().Values.Concat(new[] {newAttributeSchema})
                    .ToDictionary(x => x.Name, x => x),
                entitySchema.AssociatedData,
                entitySchema.References,
                entitySchema.EvolutionModes,
                entitySchema.GetSortableAttributeCompounds()
            );
        }

        if (existingAttributeSchema.Equals(newAttributeSchema))
        {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return entitySchema;
        }

        // ups, there is conflict in attribute settings
        throw new InvalidSchemaMutationException(
            "The attribute `" + Name + "` already exists in entity `" + entitySchema.Name + "` schema and" +
            " it has different definition. To alter existing attribute schema you need to use different mutations."
        );
    }
}
