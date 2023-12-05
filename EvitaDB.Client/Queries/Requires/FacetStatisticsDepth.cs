namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// This enum controls whether <see cref="FacetSummary"/> should contain only basic statistics about facets - e.g. count only,
/// or whether the selection impact should be computed as well.
/// </summary>
public enum FacetStatisticsDepth
{
    /// <summary>
    /// Only counts of facets will be computed.
    /// </summary>
    Counts,
    /// <summary>
    /// Counts and selection impact for non-selected facets will be computed.
    /// </summary>
    Impact
}
