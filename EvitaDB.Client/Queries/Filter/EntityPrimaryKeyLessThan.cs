namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `entityPrimaryKeyLessThan` constraint accepts an entity when the entity primary key is lesser than the passed value.
/// </summary>
public class EntityPrimaryKeyLessThan : AbstractFilterConstraintLeaf
{
    private EntityPrimaryKeyLessThan(params object?[] arguments) : base(arguments)
    {
    }

    public EntityPrimaryKeyLessThan(int primaryKey) : base(primaryKey)
    {
    }
}
