namespace EvitaDB.Client.Queries.Filter;

public class ReferenceHaving : AbstractFilterConstraintContainer
{
    private ReferenceHaving(object[] arguments, params IFilterConstraint?[] children) : base(arguments, children)
    {
    }
    
    private ReferenceHaving(string referenceName) : base(new object[] {referenceName})
    {
    }
    
    public ReferenceHaving(string referenceName, params IFilterConstraint?[] filter) : base(new object[] {referenceName}, filter)
    {
    }
    
    public string ReferenceName => (string) Arguments[0]!;
    
    public new bool Necessary => Arguments.Length == 1 && Children.Length == 1;
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return Children.Length == 0 ? new ReferenceHaving(ReferenceName) : new ReferenceHaving(ReferenceName, children);
    }
}