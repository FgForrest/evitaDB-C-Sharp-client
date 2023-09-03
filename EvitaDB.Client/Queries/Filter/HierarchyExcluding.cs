using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

public class HierarchyExcluding : AbstractFilterConstraintContainer, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "excluding";
    
    public HierarchyExcluding(params IFilterConstraint[] filtering) : base(ConstraintName, NoArguments, filtering)
    {
    }
    
    public IFilterConstraint[] Filtering => Children;
    
    public override bool Applicable => Children.Length > 0;
    public new bool Necessary => Children.Length > 0;
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children, IConstraint[] additionalChildren)
    {
        Assert.IsTrue(
            additionalChildren.Length == 0,
            "Constraint HierarchyExcluding doesn't accept other than filtering constraints!"
        );
        return new HierarchyExcluding(children);
    }
}