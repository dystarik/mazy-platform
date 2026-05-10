namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using TUnit.Core;

public static class GlobalHooks
{
    [Before(Assembly)]
    public static async Task StartBotManagerAsync()
    {
        await BotManagerFixture.Shared.StartAsync();
    }

    [After(Assembly)]
    public static async Task StopBotManagerAsync()
    {
        await BotManagerFixture.Shared.DisposeAsync();
    }
}
