namespace EvitaDB.Client.Queries.Filter;

public abstract class AbstractAttributeFilterConstraintLeaf : AbstractFilterConstraintLeaf
{
    public string AttributeName => (string) Arguments[0]!;
    protected AbstractAttributeFilterConstraintLeaf(params object[] arguments) : base(arguments)
    {
    }
}