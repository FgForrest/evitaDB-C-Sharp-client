namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `referenceHaving` constraint eliminates entities which has no reference of particular name satisfying set of
/// filtering constraints. You can examine either the attributes specified on the relation itself or wrap the filtering
/// constraint in <see cref="EntityHaving"/> constraint to examine the attributes of the referenced entity.
/// The constraint is similar to SQL <a href="https://www.w3schools.com/sql/sql_exists.asp">`EXISTS`</a> operator.
/// Example (select entities having reference brand with category attribute equal to alternativeProduct):
/// <code>
/// referenceHavingAttribute(
///     "brand",
///     attributeEquals("category", "alternativeProduct")
/// )
/// </code>
/// Example (select entities having any reference brand):
/// <code>
/// referenceHavingAttribute("brand")
/// </code>
/// Example (select entities having any reference brand of primary key 1):
/// <code>
/// referenceHavingAttribute(
///     "brand",
///     entityPrimaryKeyInSet(1)
/// )
/// </code>
/// </summary>
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
