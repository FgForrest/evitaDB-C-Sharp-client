namespace EvitaDB.Client;

public class EvitaClientTransaction : IDisposable
{
    private readonly EvitaClientSession _session;
    private readonly long _id;
    public bool RollbackOnly { get; private set; }
    public bool Closed { get; private set; }

    public EvitaClientTransaction(EvitaClientSession session, long id)
    {
        _session = session;
        _id = id;
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
        _session.CloseTransaction();
    }

    public void Dispose()
    {
        Close();
    }
}