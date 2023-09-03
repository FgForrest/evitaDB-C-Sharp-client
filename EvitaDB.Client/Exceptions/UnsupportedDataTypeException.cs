using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Exceptions;

public class UnsupportedDataTypeException : EvitaInvalidUsageException
{
    public UnsupportedDataTypeException(string privateMessage, string publicMessage) : base(privateMessage,
        publicMessage)
    {
    }

    public UnsupportedDataTypeException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public UnsupportedDataTypeException(string privateMessage, string publicMessage, Exception exception) : base(
        privateMessage, publicMessage, exception)
    {
    }

    public UnsupportedDataTypeException(string publicMessage) : base(publicMessage)
    {
    }

    public UnsupportedDataTypeException(Type type) : base(
        $"Unsupported data type: {type.FullName}. Only these types are known to Evita: {string.Join(", ", EvitaDataTypes.SupportedTypes.Select(t => t.FullName))}.")
    {
    }
}