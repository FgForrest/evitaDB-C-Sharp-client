namespace EvitaDB.Client.Exceptions;

public class InvalidSchemaException : SchemaAlteringException
{
    public InvalidSchemaException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public InvalidSchemaException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public InvalidSchemaException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public InvalidSchemaException(string publicMessage) : base(publicMessage)
    {
    }
}
