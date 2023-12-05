namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The enumeration controls <see cref="HierarchyOfReference"/> behaviour whether the hierarchical nodes that are not referred
/// by any of the queried entities should be part of the result hierarchy statistics tree.
/// </summary>
public enum EmptyHierarchicalEntityBehaviour
{
    LeaveEmpty,
    RemoveEmpty
}
