namespace EvitaDB.Client.DataTypes.Data;

public sealed record DataItemValue(object? Value) : IDataItem
{
    public void Accept(IDataItemVisitor visitor)
    {
        visitor.Visit(this);
    }

    public bool Empty => false;

    public bool Equals(DataItemValue? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null || GetType() != other.GetType()) return false;
        return Equals(Value, other.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }
}