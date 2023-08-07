using Client.Models.Mutations;

namespace Client.Models.Data;

public interface IBuilder<out T, out TM> where TM : IMutation
{
    IEnumerable<TM> BuildChangeSet();
    T Build();
}