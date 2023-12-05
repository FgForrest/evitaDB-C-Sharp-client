namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The constraint allows to sort output entities by primary key values in the exact order that is specified in
/// the arguments of this constraint.
/// Example usage:
/// <code>
/// query(
///    filterBy(
///       attributeEqualsTrue("shortcut")
///    ),
///    orderBy(
///       entityPrimaryKeyExact(5, 1, 8)
///    )
/// )
/// </code>
/// The example will return the selected entities (if present) in the exact order that is stated in the argument of
/// this ordering constraint. If there are entities, whose primary keys are not present in the argument, then they
/// will be present at the end of the output in ascending order of their primary keys (or they will be sorted by
/// additional ordering constraint in the chain).
/// </summary>
public class EntityPrimaryKeyExact : AbstractOrderConstraintLeaf
{
    private EntityPrimaryKeyExact(params object?[] args) : base(args)
    {
    }
    
    public EntityPrimaryKeyExact(params int[] primaryKeys) : base(primaryKeys.Cast<object>().ToArray())
    {
    }
    
    public int[] PrimaryKeys => Arguments.Select(x=> (int) x!).ToArray();
}
