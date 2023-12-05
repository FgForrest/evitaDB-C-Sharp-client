namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// Represents constant or "special" value attribute can have (or has it implicitly, e.g. missing value is represented by
/// `<see cref="null"/>` that is not comparable by another ways.
/// 
/// </summary>
/// <seealso cref="AttributeIs"/>
public enum AttributeSpecialValue
{
    Null,
    NotNull
}
