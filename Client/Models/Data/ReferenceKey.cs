namespace Client.Models.Data;

public record ReferenceKey(string ReferenceName, int PrimaryKey) : IComparable<ReferenceKey>
{
    public int CompareTo(ReferenceKey? other)
    {
        int comparison = PrimaryKey.CompareTo(other?.PrimaryKey ?? 0);
        if (comparison == 0)
        {
            return string.Compare(ReferenceName, other?.ReferenceName ?? string.Empty, StringComparison.Ordinal);
        }
        return comparison;
    }
}