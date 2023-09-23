namespace EvitaDB.Client.DataTypes;

/// <summary>
/// Predecessor is a special data type allowing to create consistent or semi-consistent linked lists in evitaDB and sort
/// by the order of the elements in the list.
/// </summary>
/// <param name="PredecessorId">ID if an entity that is ordered before this entity</param>
public record Predecessor(int PredecessorId)
{
    public static readonly Predecessor Head = new();
    public Predecessor() : this(-1)
    {
    }

    /// <summary>
    /// Returns true if this is the head of the list.
    /// </summary>
    public bool IsHead => PredecessorId == -1;
}