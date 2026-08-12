namespace EvitaDB.Client.DataTypes;

/// <summary>
/// Scope the entity may reside in. Entities in the <see cref="Live"/> scope represent the active data set, entities
/// in the <see cref="Archived"/> scope are soft-deleted - they are excluded from standard queries and indexes unless
/// the query explicitly targets the archived scope, but can be restored back to the live scope at any time.
/// </summary>
public enum Scope
{
    /// <summary>
    /// Entities that are currently active and reside in the live data set block.
    /// </summary>
    Live,

    /// <summary>
    /// Entities that are archived (soft-deleted).
    /// </summary>
    Archived
}
