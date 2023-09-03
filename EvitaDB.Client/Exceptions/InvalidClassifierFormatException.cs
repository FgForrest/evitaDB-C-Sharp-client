using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Exceptions;

public class InvalidClassifierFormatException : EvitaInvalidUsageException
{
    public InvalidClassifierFormatException(string privateMessage, string publicMessage) : base(privateMessage,
        publicMessage)
    {
    }

    public InvalidClassifierFormatException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public InvalidClassifierFormatException(string privateMessage, string publicMessage, Exception exception) : base(
        privateMessage, publicMessage, exception)
    {
    }

    public InvalidClassifierFormatException(string publicMessage) : base(publicMessage)
    {
    }

    public InvalidClassifierFormatException(ClassifierType classifierType, string classifier, string reason) : base(
        $"`{ClassifierTypeHelper.ToHumanReadableName(classifierType)}` `{classifier}` has invalid format. Reason: {reason}.")
    {
    }
}