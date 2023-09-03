namespace EvitaDB.Client.Exceptions;

public class TransactionNotSupportedException : EvitaInvalidUsageException
{
    public TransactionNotSupportedException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public TransactionNotSupportedException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public TransactionNotSupportedException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public TransactionNotSupportedException(string publicMessage) : base(publicMessage)
    {
    }
}