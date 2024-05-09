using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas;

public interface IAttributeSchema : INamedSchemaWithDeprecation
{
    bool Unique();
    bool UniqueWithinLocale();
    AttributeUniquenessType UniquenessType { get; }
    bool Nullable();
    bool Filterable();
    bool Sortable();
    bool Localized();
    Type Type { get; }
    Type PlainType { get; }
    object? DefaultValue { get; }
    int IndexedDecimalPlaces { get; }
}
