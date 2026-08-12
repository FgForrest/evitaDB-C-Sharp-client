namespace EvitaDB.Client.Models;

/// <summary>
/// Simplified state of a server-side task.
/// </summary>
public enum TaskSimplifiedState
{
    /// <summary>
    /// Task is waiting in the queue to be executed.
    /// </summary>
    Queued,

    /// <summary>
    /// Task is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Task has finished successfully.
    /// </summary>
    Finished,

    /// <summary>
    /// Task has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Task is waiting for a precondition to be satisfied before it can be queued.
    /// </summary>
    WaitingForPrecondition
}
