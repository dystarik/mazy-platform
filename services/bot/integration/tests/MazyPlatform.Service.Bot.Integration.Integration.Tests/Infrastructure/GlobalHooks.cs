namespace MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

using TUnit.Core;

public static class GlobalHooks
{
    [Before(Assembly)]
    public static async Task StartBotIntegrationAsync()
    {
        await BotIntegrationFixture.Shared.StartAsync();
    }

    [After(Assembly)]
    public static async Task StopBotIntegrationAsync()
    {
        await BotIntegrationFixture.Shared.DisposeAsync();
    }
}
