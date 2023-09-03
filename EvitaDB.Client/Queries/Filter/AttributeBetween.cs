namespace EvitaDB.Client.Queries.Filter;

public class AttributeBetween<T> : AbstractAttributeFilterConstraintLeaf where T : IComparable
{
    private AttributeBetween(params object?[] arguments) : base(arguments)
    {
    }

    public AttributeBetween(string attributeName, T? from, T? to) : base(attributeName, from, to)
    {
    }

    public T? From => (T?) Arguments[1];

    public T? To => (T?) Arguments[2];

    public override bool Applicable =>
        Arguments.Length == 3 && (From is not null || To is not null);
}