namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `entityPrimaryKeyLessThanEquals` constraint accepts an entity when the entity primary key is lesser than or equal to the passed value.
/// </summary>
public class EntityPrimaryKeyLessThanEquals : AbstractFilterConstraintLeaf
{
    private EntityPrimaryKeyLessThanEquals(params object?[] arguments) : base(arguments)
    {
    }

    public EntityPrimaryKeyLessThanEquals(int primaryKey) : base(primaryKey)
    {
    }
}
