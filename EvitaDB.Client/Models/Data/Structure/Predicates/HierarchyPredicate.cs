using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Models.Data.Structure.Predicates;

/// <summary>
/// This predicate allows limiting hierarchy information visible to the client based on query constraints.
/// </summary>
public class HierarchyPredicate
{
    public static readonly HierarchyPredicate DefaultInstance = new(true);
    /// <summary>
    /// Contains true if hierarchy of the entity has been fetched / requested.
    /// </summary>
    public bool RequiresHierarchy { get; }

    public HierarchyPredicate()
    {
        RequiresHierarchy = false;
    }

    public HierarchyPredicate(EvitaRequestData evitaRequestData)
    {
        RequiresHierarchy = evitaRequestData.RequiresParent;
    }

    public HierarchyPredicate(bool requiresHierarchy)
    {
        RequiresHierarchy = requiresHierarchy;
    }

    /// <summary>
    /// Returns true if the attributes were fetched along with the entity.
    /// </summary>
    public bool WasFetched() => RequiresHierarchy;

    /// <summary>
    /// Method verifies that attributes was fetched with the entity.
    /// </summary>
    /// <exception cref="ContextMissingException">thrown when hierarchy wasn't requested</exception>
    public void CheckFetched()
    {
        if (!RequiresHierarchy)
        {
            throw ContextMissingException.HierarchyContextMissing();
        }
    }
}