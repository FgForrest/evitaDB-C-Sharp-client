﻿using Client.DataTypes;
using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Attributes;

public class CreateGlobalAttributeSchemaMutation : IGlobalAttributeSchemaMutation, ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public string Description { get; }
    public string DeprecationNotice { get; }
    public bool Unique { get; }
    public bool UniqueGlobally { get; }
    public bool Filterable { get; }
    public bool Sortable { get; }
    public bool Localized { get; }
    public bool Nullable { get; }
    public Type Type { get; }
    public object? DefaultValue { get; }
    public int IndexedDecimalPlaces { get; }

    public CreateGlobalAttributeSchemaMutation(
        string name,
        string description,
        string deprecationNotice,
        bool unique,
        bool uniqueGlobally,
        bool filterable,
        bool sortable,
        bool localized,
        bool nullable,
        Type type,
        object? defaultValue,
        int indexedDecimalPlaces
        )
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Attribute, name);
        Name = name;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Unique = unique;
        UniqueGlobally = uniqueGlobally;
        Filterable = filterable;
        Sortable = sortable;
        Localized = localized;
        Nullable = nullable;
        Type = type;
        DefaultValue = defaultValue;
        IndexedDecimalPlaces = indexedDecimalPlaces;
    }
    
    public TS? Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema) where TS : IAttributeSchema
    {
        return (TS) Convert.ChangeType(AttributeSchema.InternalBuild(
            Name,
            Description,
            DeprecationNotice,
            Unique,
            UniqueGlobally,
            Filterable,
            Sortable,
            Localized,
            Nullable,
            Type,
            DefaultValue,
            IndexedDecimalPlaces), typeof(TS));
    }

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        IGlobalAttributeSchema? newAttributeSchema = Mutate<IGlobalAttributeSchema>(catalogSchema, null);
        IGlobalAttributeSchema? existingAttributeSchema = catalogSchema?.GetAttribute(Name);
        if (existingAttributeSchema == null) {
            return CatalogSchema.InternalBuild(
                catalogSchema!.Version + 1,
                catalogSchema.Name,
                catalogSchema.NameVariants,
                catalogSchema.Description,
                catalogSchema.CatalogEvolutionModes,
                catalogSchema.GetAttributes().Values.Concat(new []{newAttributeSchema}).ToDictionary(x=>x.Name, x=>x),
                catalogSchema is CatalogSchema cs ?
                    cs.EntitySchemaAccessor :
                    _ => {
                throw new NotSupportedException(
                    "Mutated schema is not able to provide access to entity schemas!"
                );
            }
            );
        }

        if (existingAttributeSchema.Equals(newAttributeSchema)) {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return catalogSchema;
        }

        // ups, there is conflict in attribute settings
        throw new InvalidSchemaMutationException(
            "The attribute `" + Name + "` already exists in entity `" + catalogSchema.Name + "` schema and" +
            " has different definition. To alter existing attribute schema you need to use different mutations."
        );
    }
}