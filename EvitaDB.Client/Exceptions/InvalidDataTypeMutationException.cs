namespace EvitaDB.Client.Exceptions;

public class InvalidDataTypeMutationException : InvalidMutationException
{
    public Type ExpectedType { get; }
    public Type ActualType { get; }
    
    public InvalidDataTypeMutationException(string message, Type expectedType, Type actualType) : base(message)
    {
        ExpectedType = expectedType;
        ActualType = actualType;
    }
}