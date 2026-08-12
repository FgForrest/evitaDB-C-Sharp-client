namespace EvitaDB.Client.Models.Mutations.Conflicts;

/// <summary>
/// Determines how a particular schema part overrides the inherited conflict resolution behaviour.
/// </summary>
public enum ConflictResolutionOverride
{
    /// <summary>
    /// The conflict resolution is inherited from the enclosing schema.
    /// </summary>
    Inherited,

    /// <summary>
    /// Conflicts on this schema part are detected granularly.
    /// </summary>
    Granular,

    /// <summary>
    /// Any parallel change of the owning entity conflicts.
    /// </summary>
    Entity
}
