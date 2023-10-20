using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class AttributeSchema : IAttributeSchema
{
    public string Name { get; }
    public IDictionary<NamingConvention, string?> NameVariants { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public bool Unique { get; }
    public bool Filterable { get; }
    public bool Sortable { get; }
    public bool Nullable { get; }
    public bool Localized { get; }
    public Type Type { get; }
    public object? DefaultValue { get; }
    public Type PlainType { get; }
    public int IndexedDecimalPlaces { get; }

    internal static AttributeSchema InternalBuild(string name, Type type, bool localized)
    {
        return new AttributeSchema(
            name, NamingConventionHelper.Generate(name),
            null, null,
            false, false, false, localized, false,
            type, null,
            0
        );
    }

    internal static AttributeSchema InternalBuild<T>(string name, bool unique, bool filterable, bool sortable,
        bool localized, bool nullable, Type type, T? defaultValue)
    {
        if ((filterable || sortable) && typeof(decimal) == type)
        {
            throw new EvitaInvalidUsageException(
                "IndexedDecimalPlaces must be specified for attributes of type BigDecimal (attribute: " + name + ")!"
            );
        }

        return new AttributeSchema(
            name, NamingConventionHelper.Generate(name),
            null, null,
            unique, filterable, sortable, localized, nullable,
            type, defaultValue,
            0
        );
    }

    internal static AttributeSchema InternalBuild<T>(string name, string? description, string? deprecationNotice,
        bool unique, bool filterable, bool sortable, bool localized, bool nullable, Type type, T? defaultValue,
        int indexedDecimalPlaces)
    {
        return new AttributeSchema(
            name, NamingConventionHelper.Generate(name),
            description, deprecationNotice,
            unique, filterable, sortable, localized, nullable,
            type, defaultValue,
            indexedDecimalPlaces
        );
    }

    internal static AttributeSchema InternalBuild<T>(string name, IDictionary<NamingConvention, string?> nameVariants,
        string? description, string? deprecationNotice, bool unique, bool filterable, bool sortable,
        bool localized, bool nullable, Type type, T? defaultValue, int
            indexedDecimalPlaces)
    {
        return new AttributeSchema(
            name, nameVariants,
            description, deprecationNotice,
            unique, filterable, sortable, localized, nullable,
            type, defaultValue,
            indexedDecimalPlaces
        );
    }

    internal static GlobalAttributeSchema InternalBuild(
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
        return new GlobalAttributeSchema(
            name, NamingConventionHelper.Generate(name),
            description, deprecationNotice,
            unique, uniqueGlobally, filterable, sortable, localized, nullable, representative,
            type, defaultValue,
            indexedDecimalPlaces
        );
    }

    internal AttributeSchema(
        string name,
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        bool unique,
        bool filterable,
        bool sortable,
        bool localized,
        bool nullable,
        Type type,
        object? defaultValue,
        int indexedDecimalPlaces
    )
    {
        Name = name;
        NameVariants = nameVariants;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Unique = unique;
        Filterable = filterable;
        Sortable = sortable;
        Localized = localized;
        Nullable = nullable;
        Type = type;
        PlainType = Type.IsArray ? Type.GetElementType()! : Type;
        DefaultValue = EvitaDataTypes.ToTargetType(defaultValue, PlainType);
        IndexedDecimalPlaces = indexedDecimalPlaces;
    }

    public string? GetNameVariant(NamingConvention namingConvention) => NameVariants.TryGetValue(namingConvention, out string? name) ? name : null;

    public override string ToString()
    {
        return "AttributeSchema{" +
               "name='" + Name + '\'' +
               ", unique=" + Unique +
               ", filterable=" + Filterable +
               ", sortable=" + Sortable +
               ", localized=" + Localized +
               ", nullable=" + Nullable +
               ", type=" + Type +
               ", indexedDecimalPlaces=" + IndexedDecimalPlaces +
               '}';
    }
}