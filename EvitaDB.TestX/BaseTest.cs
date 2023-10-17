using EvitaDB.Client;
using Xunit.Abstractions;

namespace EvitaDB.TestX;

public abstract class BaseTest : IClassFixture<SetupFixture>, IAsyncLifetime
{
    protected readonly ITestOutputHelper _outputHelper;
    protected readonly SetupFixture _setupFixture;
    
    protected EvitaClient? _client;
    
    private const int RandomSeed = 42;
    protected static readonly Random Random = new(RandomSeed);
    protected const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss zzz";
    
    public BaseTest(ITestOutputHelper outputHelper, SetupFixture setupFixture)
    {
        _outputHelper = outputHelper;
        _setupFixture = setupFixture;
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