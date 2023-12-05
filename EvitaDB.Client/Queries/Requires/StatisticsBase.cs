namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The enum specifies whether the hierarchy statistics cardinality will be based on a complete query filter by
/// constraint or only the part without user defined filter.
/// </summary>
public enum StatisticsBase
{
    CompleteFilter,
    WithoutUserFilter
}
