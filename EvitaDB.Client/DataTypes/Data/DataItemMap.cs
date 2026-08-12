namespace EvitaDB.Client.DataTypes.Data;

public sealed record DataItemMap(Dictionary<string, IDataItem?> ChildrenIndex) : IDataItem
{
    public void ForEach(Action<string, IDataItem?, bool> action)
    {
        for (int i = 0; i < ChildrenIndex.Count; i++)
        {
            KeyValuePair<string, IDataItem?> child = ChildrenIndex.ElementAt(i);
            action.Invoke(child.Key, child.Value, i < ChildrenIndex.Count - 1);
        }
    }

    public void ForEachSorted(Action<string, IDataItem?, bool> action)
    {
        Dictionary<string, IDataItem?> sortedIndex =
            ChildrenIndex.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        for (int i = 0; i < sortedIndex.Count; i++)
        {
            KeyValuePair<string, IDataItem?> child = sortedIndex.ElementAt(i);
            action.Invoke(child.Key, child.Value, i < sortedIndex.Count - 1);
        }
    }

    public void Accept(IDataItemVisitor visitor)
    {
        visitor.Visit(this);
    }

    public bool Empty => ChildrenIndex.Count == 0;

    public int PropertyCount => ChildrenIndex.Count;

    public ISet<string> PropertyNames => ChildrenIndex.Keys.ToHashSet();

    public IDataItem? GetProperty(string propertyName) =>
        ChildrenIndex.TryGetValue(propertyName, out IDataItem? value) ? value : null;

    public override int GetHashCode()
    {
        // order-independent aggregation so that structurally equal maps produce the same hash
        int hash = 0;
        foreach (KeyValuePair<string, IDataItem?> child in ChildrenIndex)
        {
            hash ^= HashCode.Combine(child.Key, child.Value);
        }

        return hash;
    }

    public override string ToString()
    {
        return "{ " + string.Join(", ", ChildrenIndex.Select(c => $"{c.Key}: {c.Value}")) + " }";
    }

    public bool Equals(DataItemMap? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null || GetType() != other.GetType()) return false;
        if (ChildrenIndex.Count != other.ChildrenIndex.Count) return false;
        foreach (KeyValuePair<string, IDataItem?> child in ChildrenIndex)
        {
            if (!other.ChildrenIndex.TryGetValue(child.Key, out IDataItem? otherChild) ||
                !Equals(child.Value, otherChild))
            {
                return false;
            }
        }

        return true;
    }
}