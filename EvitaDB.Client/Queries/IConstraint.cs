namespace EvitaDB.Client.Queries;

public interface IConstraint
{
    string Name { get; }
    Type Type { get; }
    object?[] Arguments { get; }
    bool Applicable { get; }
    void Accept(IConstraintVisitor visitor);
    string ToString();
}