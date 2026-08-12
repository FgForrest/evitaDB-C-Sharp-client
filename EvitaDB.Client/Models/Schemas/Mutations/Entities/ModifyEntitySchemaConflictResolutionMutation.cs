using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations.Conflicts;

namespace EvitaDB.Client.Models.Schemas.Mutations.Entities;

/// <summary>
/// Mutation that changes the conflict resolution behaviour of the entire entity collection. Note: the local schema model does not represent conflict-resolution settings yet, so the local
/// application is a documented no-op - the server applies the mutation authoritatively.
/// </summary>
public class ModifyEntitySchemaConflictResolutionMutation : ILocalEntitySchemaMutation
{
    public ConflictResolution? ConflictResolution { get; }

    public Operation Operation => Operation.Upsert;

    public ModifyEntitySchemaConflictResolutionMutation(ConflictResolution? conflictResolution)
    {
        ConflictResolution = conflictResolution;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        return entitySchema;
    }
}
