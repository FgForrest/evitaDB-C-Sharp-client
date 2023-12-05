namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The constraint allows output entities to be sorted by attribute values in the exact order specified in the 2nd through
/// Nth arguments of this constraint.
/// Example usage:
/// <code>
/// query(
///    filterBy(
///       attributeEqualsTrue("shortcut")
///    ),
///    orderBy(
///       attributeSetExact("code", "t-shirt", "sweater", "pants")
///    )
/// )
/// </code>
/// The example will return the selected entities (if present) in the exact order of their `code` attributes that is
/// stated in the second to Nth argument of this ordering constraint. If there are entities, that have not the attribute
/// `code` , then they will be present at the end of the output in ascending order of their primary keys (or they will be
/// sorted by additional ordering constraint in the chain).
/// </summary>
public class AttributeSetExact : AbstractOrderConstraintLeaf
{
    private AttributeSetExact(params object?[] args) : base(args)
    {
    }

    public AttributeSetExact(string attributeName, params object[] attributeValues) 
        : base(new object[] {attributeName}.Concat(attributeValues).ToArray())
    {
    }
    
    public string AttributeName => (string) Arguments[0]!;
    
    public object[] AttributeValues => Arguments.Skip(1).ToArray()!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 1;
}
