using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `hierarchyContent` requirement allows you to access the information about the hierarchical placement of
/// the entity.
/// If no additional constraints are specified, entity will contain a full chain of parent primary keys up to the root
/// of a hierarchy tree. You can limit the size of the chain by using a stopAt constraint - for example, if you're only
/// interested in a direct parent of each entity returned, you can use a stopAt(distance(1)) constraint. The result is
/// similar to using a parents constraint, but is limited in that it doesn't provide information about statistics and
/// the ability to list siblings of the entity parents. On the other hand, it's easier to use - since the hierarchy
/// placement is directly available in the retrieved entity object.
/// If you provide a nested entityFetch constraint, the hierarchy information will contain the bodies of the parent
/// entities in the required width. The attributeContent inside the entityFetch allows you to access the attributes
/// of the parent entities, etc.
/// Example:
/// <code>
/// entityFetch(
///    hierarchyContent()
/// )
/// </code>
/// </summary>
public class HierarchyContent : AbstractRequireConstraintContainer, ISeparateEntityContentRequireContainer, IEntityContentRequire
{
    public HierarchyStopAt? StopAt => Children.FirstOrDefault(x => x is HierarchyStopAt) as HierarchyStopAt;
    public EntityFetch? EntityFetch => Children.FirstOrDefault(x => x is EntityFetch) as EntityFetch;
    
    public new bool Applicable => true;
    public bool AllRequested => Arguments.Length == 0;
    
    private HierarchyContent(IRequireConstraint?[] requirements) : base(NoArguments, requirements)
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
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint?[] additionalChildren)
    {
        Assert.IsTrue(additionalChildren.Length == 0, "Additional children are not supported for HierarchyContent!");
        return new HierarchyContent(children);
    }
}
