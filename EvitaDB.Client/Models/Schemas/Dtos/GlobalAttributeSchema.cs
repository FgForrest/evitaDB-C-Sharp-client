using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class GlobalAttributeSchema : AttributeSchema, IGlobalAttributeSchema
{
    public GlobalAttributeUniquenessType GlobalUniquenessType { get; }
    public bool Representative { get; }

    public new bool Unique => base.Unique() || UniqueGlobally;
    public bool UniqueGlobally => GlobalUniquenessType != GlobalAttributeUniquenessType.NotUnique;

    public bool UniqueGloballyWithinLocale =>
        GlobalUniquenessType == GlobalAttributeUniquenessType.UniqueWithinCatalogLocale;

    public GlobalAttributeSchema(
        string name,
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        AttributeUniquenessType? unique,
        GlobalAttributeUniquenessType? globalUniquenessType,
        bool filterable,
        bool sortable,
        bool localized,
        bool nullable,
        bool representative,
        Type type,
        object? defaultValue,
        int indexedDecimalPlaces) : base(name, nameVariants, description, deprecationNotice, unique, filterable,
        sortable, localized, nullable, type, defaultValue, indexedDecimalPlaces)
    {
        GlobalUniquenessType = globalUniquenessType ?? GlobalAttributeUniquenessType.NotUnique;
        Representative = representative;
    }

    internal static new GlobalAttributeSchema InternalBuild(
        string name,
        Type type,
        bool localized
    )
    {
        return new GlobalAttributeSchema(
            name, NamingConventionHelper.Generate(name),
            null, null,
            null, null, false, false, localized, false, false,
            type, null,
            0
        );
    }
    
    public override string ToString() {
        return "GlobalAttributeSchema{" +
               "name='" + Name + '\'' +
               ", unique=" + UniquenessType +
               ", uniqueGlobally=" + GlobalUniquenessType +
               ", filterable=" + Filterable() +
               ", sortable=" + Sortable() +
               ", localized=" + Localized() +
               ", nullable=" + Nullable() +
               ", representative=" + Representative +
               ", type=" + Type +
               ", indexedDecimalPlaces=" + IndexedDecimalPlaces +
               '}';
    }
}
