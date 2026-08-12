namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// Determines whether references to managed entities that no longer exist should be returned.
/// </summary>
public enum ManagedReferencesBehaviour
{
    /// <summary>
    /// All references are returned regardless of the referenced entity existence.
    /// </summary>
    Any,

    /// <summary>
    /// Only references to existing managed entities are returned.
    /// </summary>
    Existing
}
