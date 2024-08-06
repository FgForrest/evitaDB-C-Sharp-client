using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Test.Utils;

namespace EvitaDB.Test;

public class SetupFixture : BaseSetupFixture
{
    private readonly IList<EvitaTestSuite> _testSuites = new List<EvitaTestSuite>();

    private const int GrpcPort = 5555;
    private const int SystemApiPort = 5555;
    private const string Host = "127.0.0.1";
    private const string ImageName = $"evitadb/evitadb:{ImageVersion}";
    private const string ImageVersion = "canary";

    public override Task<EvitaClient> GetClient()
    {
        if (Clients.TryDequeue(out EvitaClient? evitaClient))
        {
            DataManipulationUtil.DeleteCreateAndSetupCatalog(evitaClient, Data.TestCatalog);
            evitaClient.Close();
            return EvitaClient.Create(evitaClient.Configuration);
        }

        return InitializeEvitaContainerAndClientClient();
    }

    public override void ReturnClient(EvitaClient client)
    {
        Clients.Enqueue(client);
    }

    public override async Task InitializeAsync()
    {
        using DockerClient client = new DockerClientConfiguration().CreateClient();
        // Get information about the locally cached image (if it exists)
        var images = await client.Images.ListImagesAsync(
            new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool> { [ImageName] = true, },
                }
            });
        if (images.Count > 0)
        {
            var localImage = images[0];

            // Get information about the remote image from the Docker registry
            ImageInspectResponse remoteImage = await client.Images.InspectImageAsync(ImageName);

            // Compare image timestamps to determine if the remote image is newer
            if (remoteImage.Created > localImage.Created)
            {
                // Pull the new image
                await client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = ImageName }, null,
                    new Progress<JSONMessage>());
            }
        }
        else
        {
            // If the image is not cached locally, simply pull it
            await client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = ImageName }, null,
                new Progress<JSONMessage>());
        }

        _ = await InitializeEvitaContainerAndClientClient(true);
    }

    public override async Task DisposeAsync()
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
            container = new ContainerBuilder()
                .WithName($"evita-{Guid.NewGuid().ToString()}")
                .WithEnvironment("EVITA_ARGS", "api.endpoints.rest.host=:5555 api.endpoints.rest.tlsMode=RELAXED api.endpoints.graphQL.host=:5555 api.endpoints.graphQL.tlsMode=RELAXED api.endpoints.gRPC.mTLS.enabled=false api.endpoints.gRPC.host=:5555 api.endpoints.gRPC.tlsMode=RELAXED api.endpoints.system.host=:5555 api.endpoints.observability.host=:5555 api.endpoints.lab.host=:5555")
                // Set the image for the container to "evitadb/evitadb".
                .WithImage(ImageName)
                // Bind ports of the container.
                .WithPortBinding(GrpcPort, true)
                .WithPortBinding(SystemApiPort, true)
                .WithWaitStrategy(
                    Wait.ForUnixContainer().UntilPortIsAvailable(GrpcPort).UntilPortIsAvailable(SystemApiPort).AddCustomWaitStrategy(new CustomWaitStrategy())
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
            .SetHost(Host)
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
