namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `entityPrimaryKeyGreaterThanEquals` constraint accepts an entity when the entity primary key is greater than or equal to the passed value.
/// </summary>
public class EntityPrimaryKeyGreaterThanEquals : AbstractFilterConstraintLeaf
{
    private EntityPrimaryKeyGreaterThanEquals(params object?[] arguments) : base(arguments)
    {
    }

    public EntityPrimaryKeyGreaterThanEquals(int primaryKey) : base(primaryKey)
    {
    }
}
