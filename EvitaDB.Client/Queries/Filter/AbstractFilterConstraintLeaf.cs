namespace EvitaDB.Client.Queries.Filter;

public abstract class AbstractFilterConstraintLeaf : ConstraintLeaf, IFilterConstraint
{
    public AbstractFilterConstraintLeaf(string? name, params object[] arguments) : base(name, arguments)
    {
    }
    protected AbstractFilterConstraintLeaf(params object?[] arguments) : base(arguments)
    {
    }
    
    public override Type Type => typeof(IFilterConstraint);

    public override bool Applicable => IsArgumentsNonNull() && Arguments.Length > 0;

    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }
    
    protected static object[] Concat(object firstArg, object[] rest) {
        object[] result = new object[rest.Length + 1];
        result[0] = firstArg;
        Array.Copy(rest, 0, result, 1, rest.Length);
        return result;
    }
    
    protected static object[] Concat(object firstArg, object secondArg, object[] rest) {
        object[] result = new object[rest.Length + 2];
        result[0] = firstArg;
        result[1] = secondArg;
        Array.Copy(rest, 0, result, 2, rest.Length);
        return result;
    }
}