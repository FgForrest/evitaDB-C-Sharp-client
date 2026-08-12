namespace EvitaDB.Client.Models.Mutations.Conflicts;

/// <summary>
/// The scope on which write conflicts between parallel transactions are detected.
/// </summary>
public enum ConflictPolicy
{
    /// <summary>
    /// No conflict detection at all - last write wins.
    /// </summary>
    None,

    /// <summary>
    /// Any two parallel transactions on the same catalog conflict.
    /// </summary>
    Catalog,

    /// <summary>
    /// Two parallel transactions conflict when they touch the same entity collection.
    /// </summary>
    Collection,

    /// <summary>
    /// Two parallel transactions conflict when they touch the same entity.
    /// </summary>
    Entity
}
