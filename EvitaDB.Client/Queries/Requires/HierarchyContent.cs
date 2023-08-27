using Client.Utils;

namespace Client.Queries.Requires;

public class HierarchyContent : AbstractRequireConstraintContainer, ISeparateEntityContentRequireContainer, IEntityContentRequire
{
    public HierarchyStopAt? StopAt => Children.FirstOrDefault(x => x is HierarchyStopAt) as HierarchyStopAt;
    public EntityFetch? EntityFetch => Children.FirstOrDefault(x => x is EntityFetch) as EntityFetch;
    
    public new bool Applicable => true;
    public bool AllRequested => Arguments.Length == 0;
    
    private HierarchyContent(IRequireConstraint[] requirements) : base(NoArguments, requirements)
    {
    }

    public HierarchyContent() : base()
    {
    }
    
    public HierarchyContent(HierarchyStopAt stopAt) : base(stopAt)
    {
    }
    
    public HierarchyContent(EntityFetch entityFetch) : base(entityFetch)
    {
    }
    
    public HierarchyContent(HierarchyStopAt? stopAt, EntityFetch? entityFetch) : base(NoArguments, new IRequireConstraint?[]{stopAt, entityFetch}.Where(x=>x is not null).Cast<IRequireConstraint>().ToArray() )
    {
    }
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint[] additionalChildren)
    {
        Assert.IsTrue(additionalChildren.Length == 0, "Additional children are not supported for HierarchyContent!");
        return new HierarchyContent(children);
    }
}