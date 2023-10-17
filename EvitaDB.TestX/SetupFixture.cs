using System.Collections.Concurrent;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Client.Models.Data;
using EvitaDB.TestX.Utils;

namespace EvitaDB.TestX;

public class SetupFixture : IAsyncLifetime
{
    private readonly IList<EvitaTestSuite> _testSuites = new List<EvitaTestSuite>();
    private readonly ConcurrentQueue<EvitaClient> _clients = new();
    
    public IDictionary<string, IList<ISealedEntity>> CreatedEntities { get; private set; } =
        new Dictionary<string, IList<ISealedEntity>>();
    
    private const int GrpcPort = 5556;
    private const int SystemApiPort = 5557;
    private const string Host = "localhost";
    private const string ImageName = "evitadb/evitadb:canary";

    public async Task<EvitaClient> GetClient()
    {
        if (_clients.TryDequeue(out EvitaClient? evitaClient))
        {
            DataManipulationUtil.DeleteCreateAndSetupCatalog(evitaClient, Data.TestCatalog);
            return evitaClient;
        }
        return await InitializeEvitaContainerAndClientClient();
    }
    
    public void ReturnClient(EvitaClient client)
    {
        _clients.Enqueue(client);
    }

    public async Task InitializeAsync()
    {
        using DockerClient client = new DockerClientConfiguration().CreateClient();
        // Get information about the locally cached image (if it exists)
        var images = await client.Images.ListImagesAsync(
            new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool>
                    {
                        [ImageName] = true,
                    },
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

    public async Task DisposeAsync()
    {
        foreach (var suite in _testSuites)
        {
            suite.Client.Dispose();
            await suite.Container.StopAsync();
        }
    }
    
    private async Task<EvitaClient> InitializeEvitaContainerAndClientClient(bool cacheCreatedEntities = false)
    {
        IContainer container;
        using (var consumer = Consume.RedirectStdoutAndStderrToConsole())
        {
            container = new ContainerBuilder()
                .WithName($"evita-{Guid.NewGuid().ToString()}")
                // Set the image for the container to "evitadb/evitadb".
                .WithImage(ImageName)
                // Bind ports of the container.
                .WithPortBinding(GrpcPort, true)
                .WithPortBinding(SystemApiPort, true)
                .WithEnvironment("EVITA_JAVA_OPTS", "-Duser.timezone=UTC")
                .WithWaitStrategy(
                    Wait.ForUnixContainer().UntilPortIsAvailable(GrpcPort).UntilPortIsAvailable(SystemApiPort))
                .WithOutputConsumer(consumer)
                // Build the container configuration.
                .Build();
        
            // Start the container.
            try
            {
                await container.StartAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        // create a evita client configuration the the running instance of evita server
        EvitaClientConfiguration configuration = new EvitaClientConfiguration.Builder()
            .SetHost(Host)
            .SetPort(container.GetMappedPublicPort(GrpcPort))
            .SetSystemApiPort(container.GetMappedPublicPort(SystemApiPort))
            .Build();

        // create a new evita client with the specified configuration
        EvitaClient evitaClient = new EvitaClient(configuration);
        IDictionary<string,IList<ISealedEntity>> createdData = DataManipulationUtil.DeleteCreateAndSetupCatalog(evitaClient, Data.TestCatalog);

        if (cacheCreatedEntities)
        {
            CreatedEntities = createdData;
        }
        
        _testSuites.Add(new EvitaTestSuite(evitaClient, container));
        return evitaClient;
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