using Client.Utils;

namespace Client.Queries.Filter;

public class HierarchyHaving : AbstractFilterConstraintContainer, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "having";
    
    public HierarchyHaving(params IFilterConstraint[] children) : base(ConstraintName, NoArguments, children)
    {
    }
    
    public IFilterConstraint[] Filtering => Children;
    
    public new bool Necessary => Children.Length > 0;
    
    public new  bool Applicable => Children.Length > 0;
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children, IConstraint[] additionalChildren)
    {
        Assert.IsTrue(
            additionalChildren.Length == 0,
            "Constraint HierarchyHaving doesn't accept other than filtering constraints!"
        );
        return new HierarchyHaving(children);
    }
}