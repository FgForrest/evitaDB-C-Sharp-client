namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The enum specifies whether the <see cref="HierarchyStatistics"/> should produce the hierarchy children count or referenced
/// entity count.
/// </summary>
public enum StatisticsType
{
    ChildrenCount,
    QueriedEntityCount
}
