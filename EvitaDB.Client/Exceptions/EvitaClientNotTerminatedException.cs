namespace EvitaDB.Client.Exceptions;

public class EvitaClientNotTerminatedException : EvitaInvalidUsageException
{
    public EvitaClientNotTerminatedException()
    : base("Evita client hasn't finished in time!")
    {
    }
}