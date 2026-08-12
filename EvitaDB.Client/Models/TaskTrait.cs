namespace EvitaDB.Client.Models;

/// <summary>
/// Traits describing how a server-side task can be manipulated.
/// </summary>
public enum TaskTrait
{
    /// <summary>
    /// Task can be manually started by the user.
    /// </summary>
    CanBeStarted,

    /// <summary>
    /// Task can be manually cancelled by the user.
    /// </summary>
    CanBeCancelled,

    /// <summary>
    /// Task needs to be manually stopped by the user (it would run forever otherwise).
    /// </summary>
    NeedsToBeStopped
}
