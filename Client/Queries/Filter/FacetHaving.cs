namespace Client.Queries.Filter;

public class FacetHaving : AbstractFilterConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;
    public new bool Necessary => Arguments.Length == 1 && Children.Length > 0;
    
    private FacetHaving(object[] arguments, params IFilterConstraint[] children) : base(arguments, children)
    {
    }
    
    private FacetHaving(string referenceName) : base(referenceName)
    {
    }

    public FacetHaving(string referenceName, params IFilterConstraint[] filter) : base(new object[] {referenceName},
        filter)
    {
    }
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint[] additionalChildren)
    {
        return children.Length == 0 ? new FacetHaving(ReferenceName) : new FacetHaving(ReferenceName, children);
    }
}