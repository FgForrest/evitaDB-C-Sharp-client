namespace EvitaDB.Client.Queries.Order;

public class AbstractOrderConstraintLeaf : ConstraintLeaf, IOrderConstraint
{
    public override Type Type => typeof(IOrderConstraint);
    public override bool Applicable => IsArgumentsNonNull() && Arguments.Length > 0;
    
    protected AbstractOrderConstraintLeaf(params object?[] arguments) : base(arguments)
    {
    }

    protected AbstractOrderConstraintLeaf(string? name, params object?[] arguments) : base(name, arguments)
    {
    }
    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }
}