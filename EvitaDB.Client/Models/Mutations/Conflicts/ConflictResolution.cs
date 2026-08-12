namespace EvitaDB.Client.Models.Mutations.Conflicts;

/// <summary>
/// Describes how write conflicts between parallel transactions are detected and resolved.
/// </summary>
/// <param name="Policy">the scope conflicts are detected on</param>
/// <param name="Granularity">entity parts participating in granular detection (when the policy is entity-level)</param>
public record ConflictResolution(ConflictPolicy Policy, GranularConflictPolicy[] Granularity);
