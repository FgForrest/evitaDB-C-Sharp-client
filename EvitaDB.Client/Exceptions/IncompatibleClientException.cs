namespace EvitaDB.Client.Exceptions;

/// <summary>
/// Thrown when the client version is newer than the evitaDB server version - such a combination is not supported
/// (an older client talking to a newer server is fine).
/// </summary>
public class IncompatibleClientException : EvitaInvalidUsageException
{
    public IncompatibleClientException(string publicMessage) : base(publicMessage)
    {
    }
}
