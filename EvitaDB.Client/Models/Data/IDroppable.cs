namespace EvitaDB.Client.Models.Data;

/// <summary>
/// This interface marks data objects that are turned into a tombstone once they are removed. Dropped data still occupy
/// the original place but may be cleaned by automatic tidy process. They can be also revived anytime by setting new
/// value. Dropped item must be handled by the system as non-existing data.
/// </summary>
public interface IDroppable : IVersioned
{
    /// <summary>
    /// Returns true if data object is removed (i.e. has tombstone flag present).
    /// </summary>
    bool Dropped { get; }
}
