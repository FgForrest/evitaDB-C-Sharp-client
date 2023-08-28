using System.Collections;

namespace QueryValidator.Utils;

public static class ConversionUtils
{
    public static IDictionary<T, object> ConvertObjectToDictionary<T>(object theObject) where T : notnull
    {
        Dictionary<T, object> map = new();
        var x = (IDictionary) theObject;
        foreach (var key in x.Keys)
        {
            if (key is not null && x[key] is not null)
            {
                map.Add((T) key, x[key]!);
            }
        }
        return map;
    }
    
    public static IList<T> ConvertObjectToList<T>(object theObject) where T : notnull
    {
        IList<T> list = new List<T>();
        var x = (IList) theObject;
        foreach (var val in x)
        {
            list.Add((T) val);
        }
        return list;
    }
}