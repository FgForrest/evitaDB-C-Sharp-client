namespace EvitaDB.Client.DataTypes;

public class PaginatedList<T> : IDataChunk<T>
{
    public List<T>? Data { get; }

    public int TotalRecordCount { get; }
    public bool IsFullyInitialized => Data != null;
    public bool First => PageNumber == 1;
    public bool Last => PageNumber >= LastPageNumber;
    public bool HasPrevious => !First;
    public bool HasNext => !Last;
    public bool SinglePage => First && Last;
    public bool Empty => TotalRecordCount == 0;
    public int LastPageNumber => (int) Math.Ceiling((double) TotalRecordCount / PageSize);
    public int FirstPageItemNumber => GetFirstPageItemNumber();
    public int LastPageItemNumber => GetLastPageItemNumber();
    public int PageNumber { get; }
    public int PageSize { get; }

    public static PaginatedList<T> EmptyList => new(1, 20, 0, new List<T>());

    public PaginatedList(int pageNumber, int pageSize, int totalRecordCount)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecordCount = totalRecordCount;
        Data = new List<T>();
    }
    
    public PaginatedList(int pageNumber, int pageSize, int totalRecordCount, List<T> data)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecordCount = totalRecordCount;
        Data = data;
    }

    private int GetFirstPageItemNumber()
    {
        return IsRequestedResultBehindLimit(PageNumber, PageSize, TotalRecordCount) ? 0 : GetFirstItemNumberForPage(PageNumber, PageSize);
    }
    
    public int GetLastPageItemNumber() {
        int result = PageNumber * PageSize - 1;
        return Math.Min(result, TotalRecordCount);
    }
    
    public static int GetFirstItemNumberForPage(int pageNumber, int pageSize) {
        int firstRecord = (pageNumber - 1) * pageSize;
        return Math.Max(firstRecord, 0);
    }
    
    public static bool IsRequestedResultBehindLimit(int pageNumber, int pageSize, int totalRecordCount) {
        return ((pageNumber - 1) * pageSize) + 1 > totalRecordCount;
    }
}