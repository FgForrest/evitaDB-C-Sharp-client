namespace Client.Exceptions;

public class UnexpectedTransactionStateException : EvitaInvalidUsageException
{
    public UnexpectedTransactionStateException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public UnexpectedTransactionStateException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public UnexpectedTransactionStateException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public UnexpectedTransactionStateException(string publicMessage) : base(publicMessage)
    {
    }
}