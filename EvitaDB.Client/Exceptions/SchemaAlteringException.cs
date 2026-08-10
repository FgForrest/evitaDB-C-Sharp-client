namespace EvitaDB.Client.Exceptions;

public abstract class SchemaAlteringException : EvitaInvalidUsageException
{
    protected SchemaAlteringException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    protected SchemaAlteringException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    protected SchemaAlteringException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    protected SchemaAlteringException(string publicMessage) : base(publicMessage)
    {
    }
}
