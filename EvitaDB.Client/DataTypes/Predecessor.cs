namespace EvitaDB.Client.DataTypes;

/// <summary>
/// Predecessor is a special data type allowing to create consistent or semi-consistent linked lists in evitaDB and sort
/// by the order of the elements in the list.
/// </summary>
/// <param name="PredecessorPk">PK if an entity that is ordered before this entity</param>
public record Predecessor(int PredecessorPk) : IChainableType
{
    public static readonly Predecessor Head = new();
    public Predecessor() : this(-1)
    {
    }

    public override string ToString()
    {
        return $"Predecessor[predecessorPk={PredecessorPk}]";
    }
}
