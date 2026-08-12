namespace EvitaDB.Client.Models;

/// <summary>
/// Record describing a file stored on the evitaDB server that can be downloaded by the client
/// (e.g. a catalog backup).
/// </summary>
public record FileForFetch(
    Guid FileId,
    string Name,
    string? Description,
    string ContentType,
    long TotalSizeInBytes,
    DateTimeOffset Created,
    string? Origin
);
