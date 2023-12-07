using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas;

public interface IAttributeSchema : INamedSchemaWithDeprecation
{
    bool Unique { get; }
    bool UniqueWithinLocale { get; }
    AttributeUniquenessType UniquenessType { get; }
    bool Nullable { get; }
    bool Filterable { get; }
    bool Sortable { get; }
    bool Localized { get; }
    Type Type { get; }
    Type PlainType { get; }
    object? DefaultValue { get; }
    int IndexedDecimalPlaces { get; }
}
