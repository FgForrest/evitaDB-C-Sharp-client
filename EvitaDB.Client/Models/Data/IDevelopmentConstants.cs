namespace EvitaDB.Client.Models.Data;

public interface IDevelopmentConstants
{
    const string TestRun = "TestRun";
    static bool IsTestRun => Environment.GetEnvironmentVariable(TestRun) is "true";
}
