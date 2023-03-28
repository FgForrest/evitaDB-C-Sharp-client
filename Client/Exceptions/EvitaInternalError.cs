namespace Client.Exceptions;

public class EvitaInternalError : ArgumentException, IEvitaError
{
    public string PrivateMessage { get; }
    public string PublicMessage { get; }
    
    public EvitaInternalError(string privateMessage, string publicMessage) : base(publicMessage)
    {
        PrivateMessage = privateMessage;
        PublicMessage = publicMessage;
    }
    
    public EvitaInternalError(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
        PrivateMessage = publicMessage;
        PublicMessage = publicMessage;
    }
    
    public EvitaInternalError(string privateMessage, string publicMessage, Exception exception) : base(publicMessage, exception)
    {
        PrivateMessage = privateMessage;
        PublicMessage = publicMessage;
    }

    public EvitaInternalError(string publicMessage) : base(publicMessage)
    {
        PublicMessage = publicMessage;
        PrivateMessage = publicMessage;
    }
}