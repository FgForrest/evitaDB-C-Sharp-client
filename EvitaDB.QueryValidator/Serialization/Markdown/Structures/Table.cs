using System.Text;
using EvitaDB.QueryValidator.Utils;

namespace EvitaDB.QueryValidator.Serialization.Markdown.Structures;

public class Table<T>
{
    public const string Separator = "|";
    public const string Whitespace = " ";
    public const string DefaultTrimmingIndicator = "~";
    public const int DefaultMinimumColumnWidth = 3;

    public const int AlignCenter = 1;
    public const int AlignLeft = 2;
    public const int AlignRight = 3;

    private List<TableRow<T>> _rows;
    private List<int> _alignments;
    private readonly bool _firstRowIsHeader;
    private const int MinimumColumnWidth = DefaultMinimumColumnWidth;
    private string _trimmingIndicator = DefaultTrimmingIndicator;

    public class Builder
    {
        private readonly Table<T> _table = new();
        private int _rowLimit;

        public Builder AddRow(params T[] objects)
        {
            TableRow<T> tableRow = new TableRow<T>(new List<T>(objects));
            _table.GetRows().Add(tableRow);
            return this;
        }

        public Builder WithAlignments(List<int> alignments)
        {
            _table.SetAlignments(alignments);
            return this;
        }

        public Builder WithRowLimit(int rowLimit)
        {
            _rowLimit = rowLimit;
            return this;
        }

        public Builder WithAlignment(int alignment)
        {
            return WithAlignments(new List<int>() {alignment});
        }

        public Table<T> Build()
        {
            if (_rowLimit > 0)
            {
                _table.Trim(_rowLimit);
            }

            return _table;
        }
    }

    private Table()
    {
        _rows = new List<TableRow<T>>();
        _alignments = new List<int>();
        _firstRowIsHeader = true;
    }

    public Table(List<TableRow<T>> rows) : this()
    {
        _rows = rows;
    }

    public Table(List<TableRow<T>> rows, List<int> alignments) : this(rows)
    {
        _alignments = alignments;
    }

    public string Serialize()
    {
        IDictionary<int, int> columnWidths = GetColumnWidths(_rows, MinimumColumnWidth);

        StringBuilder sb = new StringBuilder();

        String headerSeparator = GenerateHeaderSeparator(columnWidths, _alignments);
        bool headerSeparatorAdded = !_firstRowIsHeader;
        if (!_firstRowIsHeader)
        {
            sb.Append(headerSeparator).Append('\n');
        }

        foreach (TableRow<T> row in _rows)
        {
            for (int columnIndex = 0; columnIndex < columnWidths.Count; columnIndex++)
            {
                sb.Append(Separator);

                string value = "";
                if (row.GetColumns().Count > columnIndex)
                {
                    object valueObject = row.GetColumns()[columnIndex];
                    if (valueObject != null)
                    {
                        value = valueObject.ToString();
                    }
                }

                if (value.Equals(_trimmingIndicator))
                {
                    value = StringUtils.FillUpLeftAligned(value, _trimmingIndicator, columnWidths[columnIndex]);
                    value = StringUtils.SurroundValueWith(value, Whitespace);
                }
                else
                {
                    int alignment = GetAlignment(_alignments, columnIndex);
                    value = StringUtils.SurroundValueWith(value, Whitespace);
                    value = StringUtils.FillUpAligned(value, Whitespace, columnWidths[columnIndex] + 2, alignment);
                }

                sb.Append(value);

                if (columnIndex == row.GetColumns().Count - 1)
                {
                    sb.Append(Separator);
                }
            }

            if (_rows.IndexOf(row) < _rows.Count - 1 || _rows.Count == 1)
            {
                sb.Append('\n');
            }

            if (!headerSeparatorAdded)
            {
                sb.Append(headerSeparator).Append('\n');
                headerSeparatorAdded = true;
            }
        }

        return sb.ToString();
    }

    /**
     * Removes {@link TableRow}s from the center of this table until only the requested amount of
     * rows is left.
     *
     * @param rowsToKeep Amount of {@link TableRow}s that should not be removed
     * @return the trimmed table
     */
    public Table<T> Trim(int rowsToKeep)
    {
        _rows = Trim(this, rowsToKeep, _trimmingIndicator).GetRows();
        return this;
    }

    /**
     * Removes {@link TableRow}s from the center of the specified table until only the requested
     * amount of rows is left.
     *
     * @param table             Table to remove {@link TableRow}s from
     * @param rowsToKeep        Amount of {@link TableRow}s that should not be removed
     * @param trimmingIndicator The content that trimmed cells should be filled With
     * @return The trimmed table
     */
    public static Table<T> Trim(Table<T> table, int rowsToKeep, string trimmingIndicator)
    {
        if (table.GetRows().Count <= rowsToKeep)
        {
            return table;
        }

        int trimmedEntriesCount = table.GetRows().Count - (table.GetRows().Count - rowsToKeep);
        int trimmingStartIndex = (int) Math.Round((decimal) trimmedEntriesCount / 2) + 1;
        int trimmingStopIndex = table.GetRows().Count - trimmingStartIndex;

        List<TableRow<T>> trimmedRows = new List<TableRow<T>>();
        for (int i = trimmingStartIndex; i <= trimmingStopIndex; i++)
        {
            trimmedRows.Add(table.GetRows()[i]);
        }

        trimmedRows.ForEach(x => table.GetRows().Remove(x));

        TableRow<T> trimmingIndicatorRow = new TableRow<T>();
        for (int columnIndex = 0; columnIndex < table.GetRows()[0].GetColumns().Count; columnIndex++)
        {
            //trimmingIndicatorRow.GetColumns().Add(trimmingIndicator);
        }

        table.GetRows().Insert(trimmingStartIndex, trimmingIndicatorRow);

        return table;
    }

    public static string GenerateHeaderSeparator(IDictionary<int, int> columnWidths, List<int> alignments)
    {
        StringBuilder sb = new StringBuilder();
        for (int columnIndex = 0; columnIndex < columnWidths.Count; columnIndex++)
        {
            sb.Append(Separator);

            string value = StringUtils.FillUpLeftAligned("", "-", columnWidths[columnIndex]);

            int alignment = GetAlignment(alignments, columnIndex);
            switch (alignment)
            {
                case AlignRight:
                {
                    value = Whitespace + value + ":";
                    break;
                }
                case AlignCenter:
                {
                    value = ":" + value + ":";
                    break;
                }
                default:
                {
                    value = StringUtils.SurroundValueWith(value, Whitespace);
                    break;
                }
            }

            sb.Append(value);
            if (columnIndex == columnWidths.Count - 1)
            {
                sb.Append(Separator);
            }
        }

        return sb.ToString();
    }

    public static IDictionary<int, int> GetColumnWidths(List<TableRow<T>> rows, int minimumColumnWidth)
    {
        IDictionary<int, int> columnWidths = new Dictionary<int, int>();
        if (!rows.Any())
        {
            return columnWidths;
        }

        for (int columnIndex = 0; columnIndex < rows[0].GetColumns().Count; columnIndex++)
        {
            columnWidths.Add(columnIndex, GetMaximumItemLength(rows, columnIndex, minimumColumnWidth));
        }

        return columnWidths;
    }

    public static int GetMaximumItemLength(List<TableRow<T>> rows, int columnIndex, int minimumColumnWidth)
    {
        int maximum = minimumColumnWidth;
        foreach (TableRow<T> row in rows)
        {
            if (row.GetColumns().Count < columnIndex + 1)
            {
                continue;
            }

            T value = row.GetColumns()[columnIndex];
            if (value == null)
            {
                continue;
            }

            maximum = Math.Max(value.ToString().Length, maximum);
        }

        return maximum;
    }

    public static int GetAlignment(List<int> alignments, int columnIndex)
    {
        if (!alignments.Any())
        {
            return AlignLeft;
        }

        if (columnIndex >= alignments.Count)
        {
            columnIndex = alignments.Count - 1;
        }

        return alignments[columnIndex];
    }

    public List<TableRow<T>> GetRows()
    {
        return _rows;
    }

    public void SetRows(List<TableRow<T>> rows)
    {
        _rows = rows;
    }

    public void SetAlignments(List<int> alignments)
    {
        _alignments = alignments;
    }

    public void SetTrimmingIndicator(String trimmingIndicator)
    {
        _trimmingIndicator = trimmingIndicator;
    }
}