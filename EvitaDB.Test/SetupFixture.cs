using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Test.Utils;

namespace EvitaDB.Test;

public class SetupFixture : BaseSetupFixture
{
    private readonly IList<EvitaTestSuite> _testSuites = new List<EvitaTestSuite>();

    private const int GrpcPort = 5555;
    private const int SystemApiPort = 5555;
    /// <summary>
    /// Docker image the tests run against; the tag matches the evitaDB version this client targets and can be
    /// overridden with the EVITA_IMAGE_TAG environment variable (e.g. "canary" for bleeding edge).
    /// </summary>
    private static readonly string ImageName =
        $"evitadb/evitadb:{Environment.GetEnvironmentVariable("EVITA_IMAGE_TAG") ?? DefaultImageVersion}";
    private const string DefaultImageVersion = "2026.2.4";

    public override async Task<EvitaClient> GetClient()
    {
        if (Clients.TryDequeue(out EvitaClient? evitaClient))
        {
            if (!evitaClient.IsActive)
            {
                // a test may have closed the client it borrowed - recreate one against the same container
                evitaClient = await EvitaClient.Create(evitaClient.Configuration);
            }
            // re-seeding generates new random entities - refresh the shared cache so tests compare against
            // the data that is actually on the server
            CreatedEntities = DataManipulationUtil.DeleteCreateAndSetupCatalog(evitaClient, Data.TestCatalog);
            evitaClient.Close();
            return await EvitaClient.Create(evitaClient.Configuration);
        }

        return await InitializeEvitaContainerAndClientClient();
    }

    public override void ReturnClient(EvitaClient client)
    {
        Clients.Enqueue(client);
    }

    public override async ValueTask InitializeAsync()
    {
        // the container builder pulls a fresh image when the registry has a newer one (PullPolicy.Always)
        _ = await InitializeEvitaContainerAndClientClient(true);
    }

    public override async ValueTask DisposeAsync()
    {
        foreach (var suite in _testSuites)
        {
            await suite.Container.StopAsync();
        }

        while (Clients.TryDequeue(out EvitaClient? evitaClient))
        {
            evitaClient.Close();
        }
    }

    private async Task<EvitaClient> InitializeEvitaContainerAndClientClient(
        bool cacheCreatedEntitiesAndDestroySetupClient = false)
    {
        IContainer container;
        using (var consumer = Consume.RedirectStdoutAndStderrToConsole())
        {
            container = new ContainerBuilder(ImageName)
                .WithName($"evita-{Guid.NewGuid().ToString()}")
                // graphQL/rest/lab endpoints are disabled: their server-side schema refreshers crash on the rapid
                // catalog delete+recreate cycle these tests use and poison the engine's event pipeline (server bug),
                // wedging subsequent catalog lifecycle operations; the gRPC driver tests don't need those APIs
                .WithEnvironment("EVITA_ARGS", "api.endpoints.rest.enabled=false api.endpoints.graphQL.enabled=false api.endpoints.lab.enabled=false api.endpoints.gRPC.mTLS.enabled=false api.endpoints.gRPC.host=:5555 api.endpoints.gRPC.tlsMode=RELAXED api.endpoints.system.host=:5555 api.endpoints.observability.host=:5555")
                // Pull a fresh image when the registry has a newer one (important for the "canary"/"latest" tags).
                .WithImagePullPolicy(PullPolicy.Always)
                // Bind ports of the container.
                .WithPortBinding(GrpcPort, true)
                .WithWaitStrategy(
                    Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(GrpcPort).AddCustomWaitStrategy(new CustomWaitStrategy())
                )
                .WithOutputConsumer(consumer)
                // Build the container configuration.
                .Build();

            // Start the container.
            try
            {
                await container.StartAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        EvitaClientConfiguration configuration = new EvitaClientConfiguration.Builder()
            .SetHost(container.Hostname)
            .SetPort(container.GetMappedPublicPort(GrpcPort))
            .SetSystemApiPort(container.GetMappedPublicPort(SystemApiPort))
            .Build();

        // create a new evita client with the specified configuration
        using (EvitaClient setupClient = await EvitaClient.Create(configuration))
        {
            if (cacheCreatedEntitiesAndDestroySetupClient)
            {
                CreatedEntities = DataManipulationUtil.DeleteCreateAndSetupCatalog(setupClient, Data.TestCatalog);
            }
        }

        EvitaClient client = await EvitaClient.Create(configuration);

        _testSuites.Add(new EvitaTestSuite(client, container));
        Clients.Enqueue(client);

        return client;
    }

    private class EvitaTestSuite
    {
        public EvitaClient Client { get; }
        public IContainer Container { get; }

        public EvitaTestSuite(EvitaClient client, IContainer container)
        {
            Client = client;
            Container = container;
        }
    }
}
