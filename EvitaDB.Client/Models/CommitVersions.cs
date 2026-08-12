namespace EvitaDB.Client.Models;

/// <summary>
/// Versions the catalog (and its schema) reached when a transaction commit passed a particular phase.
/// </summary>
/// <param name="CatalogVersion">version of the catalog the committed changes are (or will become) visible in</param>
/// <param name="CatalogSchemaVersion">version of the catalog schema valid for the new catalog version</param>
public record CommitVersions(long CatalogVersion, int CatalogSchemaVersion);
