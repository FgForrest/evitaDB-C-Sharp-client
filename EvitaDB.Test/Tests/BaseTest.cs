using EvitaDB.Client;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace EvitaDB.Test.Tests;

public abstract class BaseTest : IClassFixture<SetupFixture>, IAsyncLifetime
{
    protected readonly ITestOutputHelper OutputHelper;
    protected readonly SetupFixture SetupFixture;
    
    protected EvitaClient? Client;
    
    private const int RandomSeed = 42;
    protected static readonly Random Random = new(RandomSeed);
    protected const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss zzz";
    
    protected BaseTest(ITestOutputHelper outputHelper, SetupFixture setupFixture)
    {
        OutputHelper = outputHelper;
        SetupFixture = setupFixture;
    }
    
    public async Task InitializeAsync()
    {
        Client = await SetupFixture.GetClient();
    }

    public Task DisposeAsync()
    {
        SetupFixture.ReturnClient(Client!);
        return Task.CompletedTask;
    }
}