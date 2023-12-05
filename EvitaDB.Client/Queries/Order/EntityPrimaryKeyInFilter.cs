using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The constraint allows to sort output entities by primary key values in the exact order that was used for filtering
/// them. The constraint requires presence of exactly one <see cref="EntityPrimaryKeyInSet"/> constraint in filter part of
/// the query. It uses <see cref="EntityPrimaryKeyInSet.PrimaryKeys"/> array for sorting the output of the query.
/// Example usage:
/// <code>
/// query(
///    filterBy(
///       entityPrimaryKeyInSet(5, 1, 8)
///    ),
///    orderBy(
///       entityPrimaryKeyInFilter()
///    )
/// )
/// </code>
/// The example will return the selected entities (if present) in the exact order that was used for array filtering them.
/// The ordering constraint is particularly useful when you have sorted set of entity primary keys from an external
/// system which needs to be maintained (for example, it represents a relevancy of those entities).
/// </summary>
public class EntityPrimaryKeyInFilter : AbstractOrderConstraintLeaf
{
    private EntityPrimaryKeyInFilter(params object?[] args) : base(args)
    {
    }
    
    public EntityPrimaryKeyInFilter() : base()
    {
    }

    public new bool Applicable => true;
}
