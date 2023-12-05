namespace EvitaDB.Client.Models.Data;

/// <summary>
/// This interface marks all objects that are immutable and versioned. Whenever new instance of the class instance is
/// created and takes place of another class instance (i.e. is successor of that data) its version must be increased
/// by one.
/// Versioned data are used for handling [optimistic locking](https://en.wikipedia.org/wiki/Optimistic_concurrency_control).
/// </summary>
public interface IVersioned
{
    /// <summary>
    /// Returns version of the object.
    /// </summary>
    public int Version { get; }
}
