namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `entityPrimaryKeyBetween` constraint accepts an entity when the entity primary key lies in the (inclusive) range between `from` and `to`.
/// </summary>
public class EntityPrimaryKeyBetween : AbstractFilterConstraintLeaf
{
    private EntityPrimaryKeyBetween(params object?[] arguments) : base(arguments)
    {
    }

    public EntityPrimaryKeyBetween(int? @from, int? to) : base(@from, to)
    {
    }
}
