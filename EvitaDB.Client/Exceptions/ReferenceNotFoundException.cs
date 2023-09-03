using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Exceptions;

public class ReferenceNotFoundException : EvitaInvalidUsageException
{
    private ReferenceNotFoundException(string privateMessage, string publicMessage) : base(privateMessage,
        publicMessage)
    {
    }

    private ReferenceNotFoundException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    private ReferenceNotFoundException(string privateMessage, string publicMessage, Exception exception) : base(
        privateMessage, publicMessage, exception)
    {
    }

    private ReferenceNotFoundException(string publicMessage) : base(publicMessage)
    {
    }

    public ReferenceNotFoundException(string referenceName, IEntitySchema entitySchema) : base("Reference with name `" +
        referenceName + "` is not present in schema of `" + entitySchema.Name + "` entity.")
    {
    }
}