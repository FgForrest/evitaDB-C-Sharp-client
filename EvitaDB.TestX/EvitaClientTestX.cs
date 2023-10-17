using EvitaDB.Client;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.TestX.Utils;
using Xunit.Abstractions;

namespace EvitaDB.TestX;

public class EvitaClientTestX : IClassFixture<SetupFixture>, IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly SetupFixture _setupFixture;
    
    private EvitaClient? _client;
    
    private const int RandomSeed = 42;
    
    public EvitaClientTestX(ITestOutputHelper outputHelper, SetupFixture setupFixture)
    {
        _outputHelper = outputHelper;
        _setupFixture = setupFixture;
    }
    
    [Fact]
    public void ShouldBeAbleToRunParallelClients()
    {
        EvitaClient anotherParallelClient = new EvitaClient(_client!.Configuration);
        _ = ListCatalogNames(anotherParallelClient);
        _ = ListCatalogNames(_client);
    }
    
    [Fact]
    public void ShouldBeAbleToFetchNonCachedEntitySchemaFromCatalogSchema()
    {
        EvitaClient clientWithEmptyCache = new EvitaClient(_client!.Configuration);
        clientWithEmptyCache.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntitySchema? productSchema = session.GetCatalogSchema().GetEntitySchema(Entities.Product);
                Assert.NotNull(productSchema);
            }
        );
    }
    
    private static ISet<string> ListCatalogNames(EvitaClient client)
    {
        return client.GetCatalogNames();
    }

    public async Task InitializeAsync()
    {
        _client = await _setupFixture.GetClient();
    }

    public Task DisposeAsync()
    {
        _setupFixture.ReturnClient(_client!);
        return Task.CompletedTask;
    }
}