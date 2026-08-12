using EvitaDB.Client.Session;

namespace EvitaDB.Client.Models;

/// <summary>
/// Statistics of a single entity collection within a catalog.
/// </summary>
public record EntityCollectionStatistics(
    string EntityType,
    long TotalRecords,
    long IndexCount,
    long SizeOnDiskInBytes
);

/// <summary>
/// Aggregated information about a catalog known to the server.
/// </summary>
public record CatalogStatistics(
    Guid? CatalogId,
    string CatalogName,
    CatalogState CatalogState,
    long CatalogVersion,
    long TotalRecords,
    long IndexCount,
    long SizeOnDiskInBytes,
    EntityCollectionStatistics[] EntityCollectionStatistics,
    bool ReadOnly,
    bool Unusable
);
