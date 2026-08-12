namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// Determines the level the facet group relation type applies to.
/// </summary>
public enum FacetGroupRelationLevel
{
    /// <summary>
    /// The relation type applies to facets of the same group.
    /// </summary>
    WithDifferentFacetsInGroup,

    /// <summary>
    /// The relation type applies between different facet groups.
    /// </summary>
    WithDifferentGroups
}
