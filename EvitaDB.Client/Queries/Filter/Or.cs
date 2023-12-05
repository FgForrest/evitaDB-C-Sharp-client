namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `or` container represents a <a href="https://en.wikipedia.org/wiki/Logical_disjunction">logical conjunction</a>.
/// The following query:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         or(
///             entityPrimaryKeyInSet(110066, 106742, 110513),
///             entityPrimaryKeyInSet(110066, 106742),
///             entityPrimaryKeyInSet(107546, 106742,  107546)
///         )
///     )
/// )
/// </code>
/// ... returns four results representing a combination of all primary keys used in the `entityPrimaryKeyInSet`
/// constraints.
/// </summary>
public class Or : AbstractFilterConstraintContainer
{
    public Or(params IFilterConstraint?[] children) : base(children)
    {
    }
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new Or(children);
    }
}
