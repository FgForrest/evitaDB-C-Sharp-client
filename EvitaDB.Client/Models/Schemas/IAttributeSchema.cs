namespace EvitaDB.Client.Models.Schemas;

public interface IAttributeSchema : INamedSchemaWithDeprecation
{
    bool Unique { get; }
    bool Nullable { get; }
    bool Filterable { get; }
    bool Sortable { get; }
    bool Localized { get; }
    Type Type { get; }
    Type PlainType { get; }
    object? DefaultValue { get; }
    int IndexedDecimalPlaces { get; }
}