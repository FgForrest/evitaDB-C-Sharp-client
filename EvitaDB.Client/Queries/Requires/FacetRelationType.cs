namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// Determines the boolean relation used when combining selected facets in the facet summary computation.
/// </summary>
public enum FacetRelationType
{
    /// <summary>
    /// Facets are combined by boolean OR.
    /// </summary>
    Disjunction,

    /// <summary>
    /// Facets are combined by boolean AND.
    /// </summary>
    Conjunction,

    /// <summary>
    /// Facets are combined by boolean AND NOT.
    /// </summary>
    Negation,

    /// <summary>
    /// Only a single facet may be selected (selecting another one deselects the previous).
    /// </summary>
    Exclusivity
}
