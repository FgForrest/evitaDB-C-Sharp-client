namespace EvitaDB.Client.Models;

/// <summary>
/// Aggregated information about the evitaDB server instance.
/// </summary>
public record ServerStatus(
    string Version,
    DateTimeOffset? StartedAt,
    long UptimeInSeconds,
    string InstanceId,
    int CatalogsCorrupted,
    int CatalogsActive,
    int CatalogsInactive,
    HealthProblem[] HealthProblems,
    Readiness Readiness,
    bool ReadOnly
);
