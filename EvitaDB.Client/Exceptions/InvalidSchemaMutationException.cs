namespace EvitaDB.Client.Exceptions;

public class InvalidSchemaMutationException : SchemaAlteringMutation
{
    public InvalidSchemaMutationException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public InvalidSchemaMutationException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public InvalidSchemaMutationException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public InvalidSchemaMutationException(string publicMessage) : base(publicMessage)
    {
    }
}