using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class CreateGlobalAttributeSchemaMutation : IGlobalAttributeSchemaMutation, ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public bool Unique { get; }
    public bool UniqueGlobally { get; }
    public bool Filterable { get; }
    public bool Sortable { get; }
    public bool Localized { get; }
    public bool Nullable { get; }
    public bool Representative { get; }
    public Type Type { get; }
    public object? DefaultValue { get; }
    public int IndexedDecimalPlaces { get; }

    public CreateGlobalAttributeSchemaMutation(
        string name,
        string? description,
        string? deprecationNotice,
        bool unique,
        bool uniqueGlobally,
        bool filterable,
        bool sortable,
        bool localized,
        bool nullable,
        bool representative,
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
        Representative = representative;
        Type = type;
        DefaultValue = defaultValue;
        IndexedDecimalPlaces = indexedDecimalPlaces;
    }
    
    public TS Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema, Type schemaType) where TS : class, IAttributeSchema
    {
        return (AttributeSchema.InternalBuild(
            Name,
            Description,
            DeprecationNotice,
            Unique,
            UniqueGlobally,
            Filterable,
            Sortable,
            Localized,
            Nullable,
            Representative,
            Type,
            DefaultValue,
            IndexedDecimalPlaces) as TS)!;
    }

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        IGlobalAttributeSchema newAttributeSchema = Mutate<IGlobalAttributeSchema>(catalogSchema, null, typeof(IGlobalAttributeSchema));
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
                    _ => throw new NotSupportedException(
                        "Mutated schema is not able to provide access to entity schemas!"
                    ));
        }

        if (existingAttributeSchema.Equals(newAttributeSchema)) {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return catalogSchema;
        }

        // ups, there is conflict in attribute settings
        throw new InvalidSchemaMutationException(
            "The attribute `" + Name + "` already exists in entity `" + catalogSchema?.Name + "` schema and" +
            " has different definition. To alter existing attribute schema you need to use different mutations."
        );
    }
}