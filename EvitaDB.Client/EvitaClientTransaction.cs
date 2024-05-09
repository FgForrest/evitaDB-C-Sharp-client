namespace EvitaDB.Client;

public class EvitaClientTransaction : IDisposable
{
    private readonly Guid _transactionId;
    private readonly long _catalogVersion;
    public bool RollbackOnly { get; private set; }
    public bool Closed { get; private set; }

    public EvitaClientTransaction(Guid transactionId, long catalogVersion)
    {
        _transactionId = transactionId;
        _catalogVersion = catalogVersion;
    }

    public void SetRollbackOnly()
    {
        RollbackOnly = true;
    }

    public void Close()
    {
        if (Closed)
        {
            return;
        }
        Closed = true;
    }

    public void Dispose()
    {
        Close();
    }
}
