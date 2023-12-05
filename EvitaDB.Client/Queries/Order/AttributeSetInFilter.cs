using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The constraint allows to sort output entities by attribute values in the exact order that was used for filtering
/// them. The constraint requires presence of exactly one <see cref="AttributeInSet{T}"/> constraint in filter part of the query
/// that relates to the attribute with the same name as is used in the first argument of this constraint.
/// It uses <see cref="AttributeInSet{T}.AttributeValues"/> array for sorting the output of the query.
/// Example usage:
/// <pre>
/// query(
///    filterBy(
///       attributeInSet("code", "t-shirt", "sweater", "pants")
///    ),
///    orderBy(
///       attributeSetInFilter()
///    )
/// )
/// </pre>
/// The example will return the selected entities (if present) in the exact order of their attribute `code` that was used
/// for array filtering them. The ordering constraint is particularly useful when you have sorted set of attribute values
/// from an external system which needs to be maintained (for example, it represents a relevancy of those entities).
/// </summary>
public class AttributeSetInFilter : AbstractOrderConstraintLeaf
{
    private AttributeSetInFilter(params object?[] args) : base(args)
    {
    }
    
    public AttributeSetInFilter(string attributeName) : base(attributeName)
    {
    }
    
    public string AttributeName => (string) Arguments[0]!;
}
