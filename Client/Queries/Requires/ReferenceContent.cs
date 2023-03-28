using Client.Queries.Filter;
using Client.Queries.Order;

namespace Client.Queries.Requires;

//TODO other constructors, group fetch
public class ReferenceContent : AbstractRequireConstraintContainer, IEntityContentRequire
{
    private static readonly ReferenceContent AllReferences = new ReferenceContent();
    
    public string[] ReferencedEntityTypes => Arguments.Select(obj => (string) obj!).ToArray();
    public EntityFetch EntityRequirements => Children.OfType<EntityFetch>().FirstOrDefault();
    public new bool Necessary => true;
    public new bool Applicable => true;
    public bool AllRequested => ReferencedEntityTypes.Length == 0;
    public FilterBy? FilterBy => (FilterBy?) GetAdditionalChild(typeof(FilterBy));
    public OrderBy? OrderBy => (OrderBy?) GetAdditionalChild(typeof(OrderBy));

    private ReferenceContent(string[] referencedEntityType, IRequireConstraint[] requirements,
        IConstraint[] additionalChildren) : base(referencedEntityType, requirements, additionalChildren)
    {
    }

    public ReferenceContent() : base()
    {
    }
    
    public ReferenceContent(string referencedEntityType) : base(new [] {referencedEntityType})
    {
    }
    
    private EntityFetch? GetEntityFetch()
    {
        int childrenLength = Children.Length;
        if (childrenLength == 2)
            return (EntityFetch) Children[0];
        if (childrenLength != 1) 
            return null;
        if (Children[0].GetType() == typeof(EntityFetch))
            return (EntityFetch) Children[0];
        return null;
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint[] children,
        IConstraint[] additionalChildren)
    {
        throw new NotImplementedException();
    }
}