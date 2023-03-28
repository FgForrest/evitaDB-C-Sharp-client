using Client.DataTypes;
using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Attributes;

public class CreateAttributeSchemaMutation : IAttributeSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public bool Unique { get; }
    public bool Filterable { get; }
    public bool Sortable { get; }
    public bool Localized { get; }
    public bool Nullable { get; }
    public Type Type { get; }
    public object? DefaultValue { get; }
    public int IndexedDecimalPlaces { get; }
    
    public CreateAttributeSchemaMutation(string name, string? description, string? deprecationNotice, bool unique, bool filterable, bool sortable, bool localized, bool nullable, Type type, object? defaultValue, int indexedDecimalPlaces)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Attribute, name);
        Name = name;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Unique = unique;
        Filterable = filterable;
        Sortable = sortable;
        Localized = localized;
        Nullable = nullable;
        Type = type;
        DefaultValue = defaultValue;
        IndexedDecimalPlaces = indexedDecimalPlaces;
    }
    
    public TS? Mutate<TS>(CatalogSchema? catalogSchema, TS? attributeSchema) where TS : AttributeSchema
    {
        return (TS) AttributeSchema.InternalBuild(
            Name, Description, DeprecationNotice, Unique, Filterable, Sortable, Localized, Nullable, Type, DefaultValue,
            IndexedDecimalPlaces
        );
    }

    public EntitySchema? Mutate(CatalogSchema catalogSchema, EntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        AttributeSchema? newAttributeSchema = Mutate(catalogSchema, (AttributeSchema?) null);
        AttributeSchema? existingAttributeSchema = entitySchema?.GetAttribute(Name);
        if (existingAttributeSchema == null) {
            return EntitySchema.InternalBuild(
                entitySchema!.Version + 1,
                entitySchema.Name,
                entitySchema.Description,
                entitySchema.DeprecationNotice,
                entitySchema.WithGeneratedPrimaryKey,
                entitySchema.WithHierarchy,
                entitySchema.WithPrice,
                entitySchema.IndexedPricePlaces,
                entitySchema.Locales,
                entitySchema.Currencies,
                new List<AttributeSchema?>(entitySchema.Attributes.Values).Append(newAttributeSchema)
                    .ToDictionary(x=>x.Name, x=>x),
                entitySchema.AssociatedData,
                entitySchema.References,
                entitySchema.EvolutionModes
            );
        }
        if (existingAttributeSchema.Equals(newAttributeSchema)) {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return entitySchema;
        }
        // ups, there is conflict in attribute settings
        throw new InvalidSchemaMutationException(
            $"The attribute `{Name}` already exists in entity `{entitySchema?.Name}` schema and" +
            " has different definition. To alter existing attribute schema you need to use different mutations."
        );
    }
}