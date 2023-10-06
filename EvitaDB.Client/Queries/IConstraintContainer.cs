namespace EvitaDB.Client.Queries;

public interface IConstraintContainer<out T> : IConstraint where T : IConstraint
{
    public T[] Children { get; }
    public T[] ExplicitChildren { get; }
    public IConstraint[] AdditionalChildren { get; }
    public IConstraint[] ExplicitAdditionalChildren { get; }
    public new bool Applicable { get; }
    public int GetChildrenCount();
    public new object[]? Arguments { get; }
}