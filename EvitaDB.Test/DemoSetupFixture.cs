using EvitaDB.Client;
using EvitaDB.Client.Config;

namespace EvitaDB.Test;

public class DemoSetupFixture : BaseSetupFixture
{
    /// <summary>
    /// Server with the demo dataset the read-only tests run against; defaults to the public demo instance and
    /// can be redirected with the EVITA_DEMO_HOST / EVITA_DEMO_PORT environment variables (e.g. to a local
    /// container with the demo dataset when the public instance is not reachable).
    /// </summary>
    private static readonly EvitaClientConfiguration EvitaClientConfiguration =
        new EvitaClientConfiguration.Builder()
            .SetHost(Environment.GetEnvironmentVariable("EVITA_DEMO_HOST") ?? "demo.evitadb.io")
            .SetPort(int.TryParse(Environment.GetEnvironmentVariable("EVITA_DEMO_PORT"), out int port)
                ? port
                : 5555)
            .SetUseGeneratedCertificate(false)
            .SetUsingTrustedRootCaCertificate(true)
            .Build();

    public override async ValueTask InitializeAsync()
    {
        EvitaClient client = await EvitaClient.Create(EvitaClientConfiguration);
        Clients.Enqueue(client);
    }

    public override ValueTask DisposeAsync()
    {
        while (Clients.TryDequeue(out EvitaClient? evitaClient))
        {
            evitaClient.Close();
        }

        return ValueTask.CompletedTask;
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
