namespace EvitaDB.Client.Exceptions;

public class InvalidDataTypeMutationException : InvalidMutationException
{
    public Type ExpectedType { get; }
    public Type ActualType { get; }
    public InvalidDataTypeMutationException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public InvalidDataTypeMutationException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public InvalidDataTypeMutationException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public InvalidDataTypeMutationException(string publicMessage) : base(publicMessage)
    {
    }
    
    public InvalidDataTypeMutationException(string message, Type expectedType, Type actualType) : base(message)
    {
        ExpectedType = expectedType;
        ActualType = actualType;
    }
}