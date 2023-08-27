namespace Client.Queries.Requires;

public class Require : AbstractRequireConstraintContainer, IRequireConstraint
{
    public new bool Necessary => Applicable;
    public Require(params IRequireConstraint[] children) : base(children)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint[] children,
        IConstraint[] additionalChildren)
    {
        return new Require(children);
    }
}