namespace EvitaDB.Client.DataTypes;

public interface IChainableType
{
    public const int HeadPk = -1;

    bool IsHead => HeadPk == PredecessorPk;
    
    int PredecessorPk { get; }
}
