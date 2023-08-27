namespace Client.Exceptions;

public class DataTypeParseException : EvitaInvalidUsageException
{
    public DataTypeParseException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public DataTypeParseException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public DataTypeParseException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public DataTypeParseException(string publicMessage) : base(publicMessage)
    {
    }
}