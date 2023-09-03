namespace EvitaDB.Client.Exceptions;

public class ContextMissingException : EvitaInvalidUsageException
{
    public ContextMissingException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public ContextMissingException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public ContextMissingException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public ContextMissingException(string publicMessage) : base(publicMessage)
    {
    }

    public ContextMissingException() : base(
        "Query context is missing. You need to use method getPriceForSale(Currency, OffsetDateTime, Serializable...) " +
        "and provide the context on your own.")
    {
    }
}