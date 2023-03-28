namespace Client.Exceptions;

public class EvitaInvalidUsageException : ArgumentException, IEvitaError
{
    public string PrivateMessage { get; }
    public string PublicMessage { get; }

    public EvitaInvalidUsageException(string privateMessage, string publicMessage) : base(publicMessage)
    {
        PrivateMessage = privateMessage;
        PublicMessage = publicMessage;
    }
    
    public EvitaInvalidUsageException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
        PrivateMessage = publicMessage;
        PublicMessage = publicMessage;
    }
    
    public EvitaInvalidUsageException(string privateMessage, string publicMessage, Exception exception) : base(publicMessage, exception)
    {
        PrivateMessage = privateMessage;
        PublicMessage = publicMessage;
    }
    
    public EvitaInvalidUsageException(string publicMessage) : base(publicMessage)
    {
        PublicMessage = publicMessage;
        PrivateMessage = publicMessage;
    }
}