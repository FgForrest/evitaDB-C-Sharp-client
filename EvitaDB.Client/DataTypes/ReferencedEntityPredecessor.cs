namespace EvitaDB.Client.DataTypes;

public record ReferencedEntityPredecessor(int PredecessorPk) : IChainableType
{
    public static readonly ReferencedEntityPredecessor Head = new();
    
    public ReferencedEntityPredecessor() : this(IChainableType.HeadPk)
    {
    }
}
