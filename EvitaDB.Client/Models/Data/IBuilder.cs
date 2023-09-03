using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Models.Data;

public interface IBuilder<out T, out TM> where TM : IMutation
{
    IEnumerable<TM> BuildChangeSet();
    T Build();
}