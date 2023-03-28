namespace Client.Queries;

public interface IConstraintContainer<out T> : IConstraint where T : IConstraint
{
    public T[] Children { get; }
    public IConstraint[] AdditionalChildren { get; }
    public bool Applicable { get; }
    public int GetChildrenCount();
    public object[]? Arguments { get; }
}