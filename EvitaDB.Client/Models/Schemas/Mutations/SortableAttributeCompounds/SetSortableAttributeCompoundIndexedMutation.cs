using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;

/// <summary>
/// Mutation that makes the sortable attribute compound indexed (or non-indexed). The local schema model does not
/// represent per-scope indexing of compounds yet, so the local application is a documented no-op - the server
/// applies the mutation authoritatively; when indexed, the compound is indexed in the live scope.
/// </summary>
public class SetSortableAttributeCompoundIndexedMutation : IEntitySchemaMutation,
    IReferenceSortableAttributeCompoundSchemaMutation, ISortableAttributeCompoundSchemaMutation
{
    public string Name { get; }

    /// <summary>
    /// When true the compound is indexed (in the live scope) and can be used for sorting.
    /// </summary>
    public bool Indexed { get; }

    public Operation Operation => Operation.Upsert;

    public SetSortableAttributeCompoundIndexedMutation(string name, bool indexed)
    {
        Name = name;
        Indexed = indexed;
    }

    public SortableAttributeCompoundSchema? Mutate(
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema,
        ISortableAttributeCompoundSchema? sortableAttributeCompoundSchema
    )
    {
        // the compound indexing flag is not represented in the local schema model yet
        return sortableAttributeCompoundSchema as SortableAttributeCompoundSchema;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        // the compound indexing flag is not represented in the local schema model yet
        return entitySchema;
    }

    public IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        // the compound indexing flag is not represented in the local schema model yet
        return referenceSchema;
    }
}
