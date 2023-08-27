namespace Client.DataTypes;

public class StripList<T> : IDataChunk<T>
{
    public List<T>? Data { get; }
    
    public int Limit { get; }
    public int Offset { get; }
    
    public int TotalRecordCount { get; }
    public bool IsFullyInitialized => Data != null;
    public bool First => Offset == 0;
    public bool Last => Offset + Limit >= TotalRecordCount;
    public bool HasPrevious => !First;
    public bool HasNext => !Last;
    public bool SinglePage => First && Last;
    public bool Empty => TotalRecordCount == 0;
    
    public static StripList<T> EmptyList => new(1, 20, 0, new List<T>());

    public StripList(int offset, int limit, int totalRecordCount)
    {
        Offset = offset;
        Limit = limit;
        TotalRecordCount = totalRecordCount;
        Data = new List<T>();
    }
    
    public StripList(int offset, int limit, int totalRecordCount, List<T> data)
    {
        Offset = offset;
        Limit = limit;
        TotalRecordCount = totalRecordCount;
        Data = data;
    }
}