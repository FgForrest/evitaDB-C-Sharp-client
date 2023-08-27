namespace Client.Models.Data;

public interface IContentComparator<T>
{
    bool DiffersFrom(T? otherObject);
}