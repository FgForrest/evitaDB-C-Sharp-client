namespace EvitaDB.Client.Exceptions;

public class InvalidMutationException : EvitaInvalidUsageException
{
    public InvalidMutationException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public InvalidMutationException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public InvalidMutationException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public InvalidMutationException(string publicMessage) : base(publicMessage)
    {
    }
}