namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `and` container represents a <a href="https://en.wikipedia.org/wiki/Logical_conjunction">logical conjunction</a>.

/// The following query:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         and(
///             entityPrimaryKeyInSet(110066, 106742, 110513),
///             entityPrimaryKeyInSet(110066, 106742),
///             entityPrimaryKeyInSet(107546, 106742,  107546)
///         )
///     )
/// )
/// </code>
/// ... returns a single result - product with entity primary key 106742, which is the only one that all three
/// `entityPrimaryKeyInSet` constraints have in common.
/// </summary>
public class And : AbstractFilterConstraintContainer
{
    public And(params IFilterConstraint?[] children) : base(children)
    {
    }
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new And(children);
    }
}
