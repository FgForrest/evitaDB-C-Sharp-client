namespace Client.Queries.Requires;

public abstract class AbstractRequireConstraintLeaf : ConstraintLeaf, IRequireConstraint
{
    protected AbstractRequireConstraintLeaf(params object[] arguments) : base(arguments) {
    }

    public override Type Type => typeof(IRequireConstraint);
    public override bool Applicable => true;
    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }

    protected static object[] Concat(object firstArg, object[] rest)
    {
        object[] result = new object [rest.Length + 1];
        result[0] = firstArg;
        Array.Copy(rest, 0, result, 1, rest.Length);
        return result;
    }
}