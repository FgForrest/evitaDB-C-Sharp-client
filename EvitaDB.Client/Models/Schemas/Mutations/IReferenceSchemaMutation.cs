namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface IReferenceSchemaMutation : ISchemaMutation
{
    string Name { get; }

    IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema);
}