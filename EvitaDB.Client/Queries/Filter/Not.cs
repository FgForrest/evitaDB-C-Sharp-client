namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `not` container represents a <a href="https://en.wikipedia.org/wiki/Negation">logical negation</a>.
/// The following query:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         not(
///             entityPrimaryKeyInSet(110066, 106742, 110513)
///         )
///     )
/// )
/// </code>
/// ... returns thousands of results excluding the entities with primary keys mentioned in `entityPrimaryKeyInSet`
/// constraint. Because this situation is hard to visualize - let"s narrow our super set to only a few entities:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         entityPrimaryKeyInSet(110513, 66567, 106742, 66574, 66556, 110066),
///         not(
///             entityPrimaryKeyInSet(110066, 106742, 110513)
///         )
///     )
/// )
/// </code>
/// ... which returns only three products that were not excluded by the following `not` constraint.
/// </summary>
public class Not : AbstractFilterConstraintContainer
{
    private Not()
    {
    }
    
    public Not(IFilterConstraint? child) : base(child)
    {
    }

    public new bool Necessary => Children.Length > 0;
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return children.Length == 0 ? new Not() : new Not(children[0]);
    }
}
