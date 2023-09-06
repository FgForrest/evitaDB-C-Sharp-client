using System.Text;
using EvitaDB.QueryValidator.Serialization.Markdown.Exceptions;
using EvitaDB.QueryValidator.Utils;

namespace EvitaDB.QueryValidator.Serialization.Markdown.Structures;

public class TableRow<T>
{
    private List<T> _columns;

    public TableRow()
    {
        _columns = new List<T>();
    }

    public TableRow(List<T> columns)
    {
        _columns = columns;
    }

    public string Serialize()
    {
        StringBuilder sb = new StringBuilder();
        foreach (T item in _columns)
        {
            if (item == null)
            {
                throw new MarkdownSerializationException("Column is null");
            }

            if (item.ToString()!.Contains(Table<object>.Separator))
            {
                throw new MarkdownSerializationException("Column contains separator char \"" + Table<object>.Separator + "\"");
            }

            sb.Append(Table<object>.Separator);
            sb.Append(StringUtils.SurroundValueWith(item.ToString()!, " "));
            if (_columns.IndexOf(item) == _columns.Count - 1)
            {
                sb.Append(Table<object>.Separator);
            }
        }

        return sb.ToString();
    }

    public List<T> GetColumns()
    {
        return _columns;
    }

    public void SetColumns(List<T> columns)
    {
        _columns = columns;
    }
}