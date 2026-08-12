using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations.Conflicts;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

/// <summary>
/// Mutation that overrides the conflict resolution behaviour of the attribute schema. Note: the local schema model does not represent conflict-resolution settings yet, so the local
/// application is a documented no-op - the server applies the mutation authoritatively.
/// </summary>
public class SetAttributeSchemaConflictResolutionOverrideMutation : IEntityAttributeSchemaMutation,
    IGlobalAttributeSchemaMutation, IReferenceAttributeSchemaMutation, ILocalCatalogSchemaMutation,
    ILocalEntitySchemaMutation
{
    public string Name { get; }

    public ConflictResolutionOverride ConflictResolutionOverride { get; }

    public Operation Operation => Operation.Upsert;

    public SetAttributeSchemaConflictResolutionOverrideMutation(string name,
        ConflictResolutionOverride conflictResolutionOverride)
    {
        Name = name;
        ConflictResolutionOverride = conflictResolutionOverride;
    }

    public TS? Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema, Type schemaType)
        where TS : class, IAttributeSchema
    {
        return attributeSchema;
    }

    public IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        return referenceSchema;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        return entitySchema;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema,
        Dtos.IEntitySchemaProvider entitySchemaProvider)
    {
        return catalogSchema is null
            ? null
            : new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(catalogSchema);
    }
}
