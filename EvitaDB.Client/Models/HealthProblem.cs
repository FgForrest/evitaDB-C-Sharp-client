namespace EvitaDB.Client.Models;

/// <summary>
/// Signalized health problems of the evitaDB server.
/// </summary>
public enum HealthProblem
{
    /// <summary>
    /// The server is running out of memory.
    /// </summary>
    MemoryShortage,

    /// <summary>
    /// At least one external API that is expected to be available is not available.
    /// </summary>
    ExternalApiUnavailable,

    /// <summary>
    /// The server input queues are filling up faster than they are consumed.
    /// </summary>
    InputQueuesOverloaded,

    /// <summary>
    /// The server encountered internal errors recently.
    /// </summary>
    JavaInternalErrors
}
