namespace Client.Queries.Requires;

public class EntityFetch : AbstractRequireConstraintContainer, IEntityFetchRequire
{
    protected EntityFetch(IRequireConstraint[] requireConstraints) : base(requireConstraints)
    {
    }

    public EntityFetch() : base()
    {
    }

    public EntityFetch(params IEntityContentRequire[] requirements) : base(requirements)
    {
    }
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint[] children, IConstraint[] additionalChildren)
    {
        return new EntityFetch(children);
    }

    public IEntityContentRequire?[] Requirements => Children.Select(x=>x as IEntityContentRequire).ToArray();
    public new bool Necessary => true;
    public new bool Applicable => true;
}