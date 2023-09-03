namespace EvitaDB.Client.Exceptions;

public abstract class SchemaAlteringMutation : EvitaInvalidUsageException
{
    protected SchemaAlteringMutation(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    protected SchemaAlteringMutation(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    protected SchemaAlteringMutation(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    protected SchemaAlteringMutation(string publicMessage) : base(publicMessage)
    {
    }
}