namespace EvitaDB.Client.Exceptions;

public class EvitaInternalError : ArgumentException, IEvitaError
{
    public string PrivateMessage { get; }
    public string PublicMessage { get; }
    public string ErrorCode { get; }

    public static EvitaInternalError CreateExceptionWithErrorCode(string publicMessage, string errorCode)
    {
        return new EvitaInternalError(publicMessage, publicMessage, errorCode);
    }

    public EvitaInternalError(string privateMessage, string publicMessage) : base(publicMessage)
    {
        PrivateMessage = privateMessage;
        PublicMessage = publicMessage;
        ErrorCode = Environment.StackTrace;
    }

    public EvitaInternalError(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
        PrivateMessage = publicMessage;
        PublicMessage = publicMessage;
        ErrorCode = Environment.StackTrace;
    }

    public EvitaInternalError(string privateMessage, string publicMessage, Exception exception) : base(publicMessage,
        exception)
    {
        PrivateMessage = privateMessage;
        PublicMessage = publicMessage;
        ErrorCode = Environment.StackTrace;
    }

    public EvitaInternalError(string publicMessage) : base(publicMessage)
    {
        PublicMessage = publicMessage;
        PrivateMessage = publicMessage;
        ErrorCode = Environment.StackTrace;
    }

    private EvitaInternalError(string privateMessage, string publicMessage, string errorCode) : base(privateMessage)
    {
        PublicMessage = publicMessage;
        PrivateMessage = privateMessage;
        ErrorCode = errorCode;
    }
}