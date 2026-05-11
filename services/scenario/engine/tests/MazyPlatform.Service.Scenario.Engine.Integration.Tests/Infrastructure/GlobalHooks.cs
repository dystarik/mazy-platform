namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

using TUnit.Core;

public static class GlobalHooks
{
    [Before(Assembly)]
    public static async Task StartScenarioEngineAsync()
    {
        await ScenarioEngineFixture.Shared.StartAsync();
    }

    [After(Assembly)]
    public static async Task StopScenarioEngineAsync()
    {
        await ScenarioEngineFixture.Shared.DisposeAsync();
    }
}
