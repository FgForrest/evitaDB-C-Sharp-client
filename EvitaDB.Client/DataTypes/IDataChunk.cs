using System.Collections;

namespace EvitaDB.Client.DataTypes;

public interface IDataChunk<T> : IEnumerable<T>
{
    List<T>? Data { get; }
    int TotalRecordCount { get; }
    bool IsFullyInitialized { get; }
    bool First { get; }
    bool Last { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
    bool SinglePage { get; }
    bool Empty { get; }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Data?.GetEnumerator() ?? new List<T>.Enumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Data?.GetEnumerator() ?? new List<T>.Enumerator();
    }
}