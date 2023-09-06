using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Exceptions;

public class AssociatedDataNotFoundException : EvitaInvalidUsageException
{
    public AssociatedDataNotFoundException(string associatedDataName, IEntitySchema entitySchema) : base(
        "Associated data with name `" + associatedDataName + "` is not present in schema of `" + entitySchema.Name +
        "` entity.")
    {
    }
}