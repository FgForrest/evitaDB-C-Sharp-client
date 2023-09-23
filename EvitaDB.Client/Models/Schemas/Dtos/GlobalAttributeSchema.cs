using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class GlobalAttributeSchema : AttributeSchema, IGlobalAttributeSchema
{
    public bool UniqueGlobally { get; }

    public new bool Unique => base.Unique || UniqueGlobally;

    public GlobalAttributeSchema(
        string name,
        IDictionary<NamingConvention, string> nameVariants,
        string? description,
        string? deprecationNotice,
        bool unique,
        bool uniqueGlobally,
        bool filterable,
        bool sortable,
        bool localized,
        bool nullable,
        Type type,
        object? defaultValue,
        int indexedDecimalPlaces) : base(name, nameVariants, description, deprecationNotice, unique, filterable,
        sortable, localized, nullable, type, defaultValue, indexedDecimalPlaces)
    {
        UniqueGlobally = uniqueGlobally;
    }
    
    internal new static GlobalAttributeSchema InternalBuild(
        string name,
    Type type,
    bool localized
    ) {
        return new GlobalAttributeSchema(
            name, NamingConventionHelper.Generate(name),
            null, null,
            false, false, false, false, localized, false,
            type, null,
            0
        );
    }
}