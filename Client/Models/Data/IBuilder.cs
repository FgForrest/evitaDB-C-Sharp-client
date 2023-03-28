using Client.Models.Mutations;

namespace Client.Models.Data;

public interface IBuilder<out T>
{
    ICollection<IMutation> BuildChangeSet();
    T Build();
}