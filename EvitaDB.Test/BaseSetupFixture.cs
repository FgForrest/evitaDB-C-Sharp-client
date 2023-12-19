using System.Collections.Concurrent;
using EvitaDB.Client;
using EvitaDB.Client.Models.Data;

namespace EvitaDB.Test;

public abstract class BaseSetupFixture : IAsyncLifetime
{
    public abstract Task<EvitaClient> GetClient();
    public abstract void ReturnClient(EvitaClient client);
    public IDictionary<string, IList<ISealedEntity>> CreatedEntities { get; protected set; } = new Dictionary<string, IList<ISealedEntity>>();
    protected ConcurrentQueue<EvitaClient> Clients { get; } = new();
    public abstract Task InitializeAsync();
    public abstract Task DisposeAsync();

    public BaseSetupFixture()
    {
        
    }
}
