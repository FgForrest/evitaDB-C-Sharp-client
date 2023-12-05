namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The constraint allows to sort output entities by primary key values in the exact order.
///
/// Example usage:
/// <code>
/// query(
///    orderBy(
///       entityPrimaryKeyNatural(DESC)
///    )
/// )
/// </code>
/// The example will return the selected entities (if present) in the descending order of their primary keys. Since
/// the entities are by default ordered by their primary key in ascending order, it has no sense to use this constraint
/// with <see cref="OrderDirection.Asc"/> direction.
/// </summary>
public class EntityPrimaryKeyNatural : AbstractOrderConstraintLeaf
{
    private EntityPrimaryKeyNatural(params object[] arguments) : base(arguments)
    {
    }

    public EntityPrimaryKeyNatural(OrderDirection orderDirection) : base(orderDirection)
    {
    }

    public OrderDirection Direction => (OrderDirection)Arguments[0]!;

    public IOrderConstraint CloneWithArguments(params object[] newArguments)
    {
        return new EntityPrimaryKeyNatural(newArguments);
    }
}
