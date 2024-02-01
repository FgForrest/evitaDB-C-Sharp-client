namespace EvitaDB.Client.Utils;

public static class ArrayUtils
{
    public static bool IsEmpty(object?[]? array) => array is null || array.Length == 0;
    
    public static bool IsEmpty<T>(T[]? array) => array is null || array.Length == 0;
}
