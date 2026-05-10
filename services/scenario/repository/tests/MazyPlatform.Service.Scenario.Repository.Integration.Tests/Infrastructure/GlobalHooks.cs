namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

using TUnit.Core;

public static class GlobalHooks
{
    [Before(Assembly)]
    public static async Task StartScenarioRepositoryAsync()
    {
        await ScenarioRepositoryFixture.Shared.StartAsync();
    }

    [After(Assembly)]
    public static async Task StopScenarioRepositoryAsync()
    {
        await ScenarioRepositoryFixture.Shared.DisposeAsync();
    }
}
