namespace Client.DataTypes.Data;

public sealed record DataItemArray(IDataItem?[] Children) : IDataItem
{
    public void Accept(IDataItemVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void ForEach(Action<IDataItem?, bool> action)
    {
        for (int i = 0; i < Children.Length; i++)
        {
            IDataItem? child = Children[i];
            action.Invoke(child, i < Children.Length - 1);
        }
    }

    public bool Empty => Children.Length == 0;
    
    public bool Equals(DataItemArray? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null || GetType() != other.GetType()) return false;
        return Equals(Children, other.Children);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Children);
    }

    public override string ToString()
    {
        return $"[{string.Join(", ", Children.Select(c => c.ToString()))}]";
    }
}