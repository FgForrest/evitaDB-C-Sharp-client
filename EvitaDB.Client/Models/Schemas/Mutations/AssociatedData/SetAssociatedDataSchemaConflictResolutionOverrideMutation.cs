using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations.Conflicts;

namespace EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

/// <summary>
/// Mutation that overrides the conflict resolution behaviour of the associated data schema. Note: the local schema model does not represent conflict-resolution settings yet, so the local
/// application is a documented no-op - the server applies the mutation authoritatively.
/// </summary>
public class SetAssociatedDataSchemaConflictResolutionOverrideMutation : ILocalEntitySchemaMutation
{
    public string Name { get; }

    public ConflictResolutionOverride ConflictResolutionOverride { get; }

    public Operation Operation => Operation.Upsert;

    public SetAssociatedDataSchemaConflictResolutionOverrideMutation(string name,
        ConflictResolutionOverride conflictResolutionOverride)
    {
        Name = name;
        ConflictResolutionOverride = conflictResolutionOverride;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        return entitySchema;
    }
}
