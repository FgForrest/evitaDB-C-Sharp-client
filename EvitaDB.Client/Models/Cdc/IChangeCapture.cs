using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Models.Cdc;

public interface IChangeCapture
{
    long Version { get; }
    int Index { get; }
    Operation Operation { get; }
    IMutation?Body { get; }
}
