namespace EvitaDB.Client.Models.Mutations.Conflicts;

/// <summary>
/// Fine-grained parts of an entity that participate in granular conflict detection.
/// </summary>
public enum GranularConflictPolicy
{
    EntityAttribute,
    Reference,
    ReferenceAttribute,
    AssociatedData,
    Price,
    Hierarchy
}
