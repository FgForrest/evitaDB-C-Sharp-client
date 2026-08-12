namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// Determines how the hierarchy tree is traversed when ordering entities by <see cref="TraverseByEntityProperty"/>.
/// </summary>
public enum TraversalMode
{
    /// <summary>
    /// The hierarchy is traversed depth first (children of a node are visited before its siblings).
    /// </summary>
    DepthFirst,

    /// <summary>
    /// The hierarchy is traversed breadth first (all nodes on a level are visited before their children).
    /// </summary>
    BreadthFirst
}
