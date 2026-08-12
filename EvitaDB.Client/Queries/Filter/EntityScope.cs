using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `scope` filtering constraint limits the query to entities in the named scope(s). By default only entities in
/// the <see cref="Scope.Live"/> scope are queried; passing <see cref="Scope.Archived"/> (alone or together with the
/// live scope) allows querying soft-deleted entities as well.
/// Example:
/// <code>
/// scope(LIVE, ARCHIVED)
/// </code>
/// </summary>
public class EntityScope : AbstractFilterConstraintLeaf
{
    private const string ConstraintName = "scope";

    public IEnumerable<Scope> Scopes => Arguments.OfType<Scope>();

    private EntityScope(params object?[] arguments) : base(ConstraintName, arguments)
    {
    }

    public EntityScope(params Scope[] scopes) : base(ConstraintName, scopes.Cast<object?>().ToArray())
    {
    }
}
