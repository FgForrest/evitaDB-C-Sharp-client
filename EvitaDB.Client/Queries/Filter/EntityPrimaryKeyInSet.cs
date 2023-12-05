namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `entityPrimaryKeyInSet` constraint limits the list of returned entities by exactly specifying their entity
/// primary keys.
/// Example:
/// <code>
/// primaryKey(1, 2, 3)
/// </code>
/// </summary>
public class EntityPrimaryKeyInSet : AbstractFilterConstraintLeaf
{
    private EntityPrimaryKeyInSet(params object?[] arguments) : base(arguments)
    {
        
    }

    public EntityPrimaryKeyInSet(params int[] primaryKeys) : base(primaryKeys.Cast<object>().ToArray())
    {
    }

    public int[] PrimaryKeys => Arguments.Select(Convert.ToInt32).ToArray();
}
