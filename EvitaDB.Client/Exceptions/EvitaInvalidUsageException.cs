namespace EvitaDB.Client.Exceptions;

public class EvitaInvalidUsageException : ArgumentException, IEvitaError
{
    public string PrivateMessage { get; }
    public string PublicMessage { get; }
    public string ErrorCode { get; }

    public static EvitaInvalidUsageException CreateExceptionWithErrorCode(string publicMessage, string errorCode)
    {
        return new EvitaInvalidUsageException(publicMessage, publicMessage, errorCode);
    }

    public EvitaInvalidUsageException(string privateMessage, string publicMessage) : base(publicMessage)
    {
        PrivateMessage = privateMessage;
        PublicMessage = publicMessage;
        ErrorCode = Environment.StackTrace;
    }

    public EvitaInvalidUsageException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
        PrivateMessage = publicMessage;
        PublicMessage = publicMessage;
        ErrorCode = Environment.StackTrace;
    }

    public EvitaInvalidUsageException(string privateMessage, string publicMessage, Exception exception) : base(
        publicMessage, exception)
    {
        PrivateMessage = privateMessage;
        PublicMessage = publicMessage;
        ErrorCode = Environment.StackTrace;
    }

    public EvitaInvalidUsageException(string publicMessage) : base(publicMessage)
    {
        PublicMessage = publicMessage;
        PrivateMessage = publicMessage;
        ErrorCode = Environment.StackTrace;
    }
    
    private EvitaInvalidUsageException(string privateMessage, string publicMessage, string errorCode) : base(privateMessage) {
        PublicMessage = publicMessage;
        ErrorCode = errorCode;
    }
}