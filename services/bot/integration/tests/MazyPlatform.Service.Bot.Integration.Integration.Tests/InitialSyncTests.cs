namespace MazyPlatform.Service.Bot.Integration.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

public sealed class InitialSyncTests : IntegrationTestBase
{
    [Test]
    public async Task InitialSync_Should_CallGetActiveBotsForTelegramAndVk_WithInternalToken()
    {
        await App.BotManager.WaitForGetActiveBotsCallsAsync(2);

        var calls = App.BotManager.GetActiveBotsCalls;

        await Assert.That(calls.Any(x => x.PlatformType == BotPlatformType.Telegram)).IsTrue();
        await Assert.That(calls.Any(x => x.PlatformType == BotPlatformType.Vk)).IsTrue();
        await Assert.That(calls.All(x => string.Equals(x.InternalToken, BotIntegrationFixture.BotManagerAccessToken, StringComparison.Ordinal))).IsTrue();

        using var ready = await App.GetHealthAsync("/health/ready");
        await Assert.That(ready.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task InitialSync_WithInvalidActiveBotItems_Should_NotBreakStartup()
    {
        await using var fixture = await BotIntegrationFixture.StartIsolatedAsync(botManager =>
        {
            botManager.AddActiveBot(BotPlatformType.Telegram, BotIntegrationTestData.MalformedBotId());
            botManager.AddActiveBot(BotPlatformType.Telegram, BotIntegrationTestData.MalformedProjectId());
            botManager.AddActiveBot(BotPlatformType.Telegram, BotIntegrationTestData.MissingCredentials());
            botManager.AddActiveBot(BotPlatformType.Vk, BotIntegrationTestData.VkWithNonNumericCommunityId());
        });

        await fixture.BotManager.WaitForGetActiveBotsCallsAsync(2);
        using var ready = await fixture.GetHealthAsync("/health/ready");

        await Assert.That(ready.IsSuccessStatusCode).IsTrue();
    }
}
