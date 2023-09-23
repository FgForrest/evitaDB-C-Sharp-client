using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Exceptions;

public class ReferenceNotFoundException : EvitaInvalidUsageException
{
    public ReferenceNotFoundException(string referenceName, IEntitySchema entitySchema) : base("Reference with name `" +
        referenceName + "` is not present in schema of `" + entitySchema.Name + "` entity.")
    {
    }
}