namespace EvitaDB.Client.Models;

/// <summary>
/// Aggregated readiness state of the server APIs.
/// </summary>
public enum Readiness
{
    /// <summary>
    /// At least one API is not yet ready.
    /// </summary>
    ApiStarting,

    /// <summary>
    /// All APIs are ready.
    /// </summary>
    ApiReady,

    /// <summary>
    /// At least one API that was ready before is not ready now.
    /// </summary>
    ApiStalling,

    /// <summary>
    /// The server is shutting down; no API is ready.
    /// </summary>
    ApiShutdown,

    /// <summary>
    /// The state is not yet known (the server probably starts up).
    /// </summary>
    ApiUnknown
}
