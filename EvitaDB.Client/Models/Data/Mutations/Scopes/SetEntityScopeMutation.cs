using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Scopes;

/// <summary>
/// Mutation that changes the scope of the entity - i.e. moves it between the <see cref="Scope.Live"/> and
/// <see cref="Scope.Archived"/> data sets (soft delete / restore).
/// </summary>
public class SetEntityScopeMutation : ILocalMutation<Scope>
{
    public Scope Scope { get; }

    public Operation Operation => Operation.Upsert;

    public SetEntityScopeMutation(Scope scope)
    {
        Scope = scope;
    }

    public Scope MutateLocal(IEntitySchema entitySchema, Scope existingValue)
    {
        return Scope;
    }
}
