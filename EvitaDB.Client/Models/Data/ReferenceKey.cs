namespace EvitaDB.Client.Models.Data;

public record ReferenceKey(string ReferenceName, int PrimaryKey) : IComparable<ReferenceKey>
{
    public int CompareTo(ReferenceKey? other)
    {
        int comparison = PrimaryKey.CompareTo(other?.PrimaryKey ?? 0);
        return comparison == 0
            ? string.Compare(ReferenceName, other?.ReferenceName ?? string.Empty, StringComparison.Ordinal)
            : comparison;
    }
}