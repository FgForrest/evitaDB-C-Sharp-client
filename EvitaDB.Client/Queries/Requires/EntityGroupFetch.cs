namespace Client.Queries.Requires;

public class EntityGroupFetch : AbstractRequireConstraintContainer, IEntityFetchRequire
{
    private EntityGroupFetch(IRequireConstraint[] requireConstraints) : base(requireConstraints)
    {
    }
    
    public EntityGroupFetch() : base()
    {
    }
    
    public EntityGroupFetch(params IEntityContentRequire[] requirements) : base(requirements)
    {
    }
    
    public new bool Applicable => true;
    
    public IEntityContentRequire?[] Requirements => Children
        .Select(x => x as IEntityContentRequire)
        .ToArray();
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint[] additionalChildren)
    {
        return new EntityGroupFetch(children);
    }
}