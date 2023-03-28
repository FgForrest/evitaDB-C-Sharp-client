using System.Collections;

namespace Client.DataTypes;

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
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}