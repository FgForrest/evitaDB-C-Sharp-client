using EvitaDB.Client;
using EvitaDB.Client.Config;

namespace EvitaDB.Test;

public class DemoSetupFixture : BaseSetupFixture
{
    private static readonly EvitaClientConfiguration EvitaClientConfiguration =
        new EvitaClientConfiguration.Builder()
            .SetHost("demo.evitadb.io")
            .SetPort(5556)
            .SetUseGeneratedCertificate(false)
            .SetUsingTrustedRootCaCertificate(true)
            .Build();

    public override async Task InitializeAsync()
    {
        EvitaClient client = await EvitaClient.Create(EvitaClientConfiguration);
        Clients.Enqueue(client);
    }

    public override Task DisposeAsync()
    {
        while (Clients.TryDequeue(out EvitaClient? evitaClient))
        {
            evitaClient.Close();
        }

        return Task.CompletedTask;
    }

    public override async Task<EvitaClient> GetClient()
    {
        if (Clients.TryDequeue(out EvitaClient? evitaClient))
        {
            return evitaClient;
        }

        return await EvitaClient.Create(EvitaClientConfiguration);
    }

    public override void ReturnClient(EvitaClient client)
    {
        Clients.Enqueue(client);
    }
}
