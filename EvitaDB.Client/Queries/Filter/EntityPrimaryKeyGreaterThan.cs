namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `entityPrimaryKeyGreaterThan` constraint accepts an entity when the entity primary key is greater than the passed value.
/// </summary>
public class EntityPrimaryKeyGreaterThan : AbstractFilterConstraintLeaf
{
    private EntityPrimaryKeyGreaterThan(params object?[] arguments) : base(arguments)
    {
    }

    public EntityPrimaryKeyGreaterThan(int primaryKey) : base(primaryKey)
    {
    }
}
