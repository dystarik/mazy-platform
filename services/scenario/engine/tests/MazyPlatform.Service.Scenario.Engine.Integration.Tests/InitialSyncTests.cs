namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class InitialSyncTests : IntegrationTestBase
{
    [Test]
    public async Task InitialSync_Should_CallGetActiveBotsForTelegramAndVk_WithInternalToken()
    {
        await App.BotManager.WaitForGetActiveBotsCallsAsync(2);

        var calls = App.BotManager.GetActiveBotsCalls;

        await Assert.That(calls.Any(x => x.PlatformType == BotPlatformType.Telegram)).IsTrue();
        await Assert.That(calls.Any(x => x.PlatformType == BotPlatformType.Vk)).IsTrue();
        await Assert.That(calls.All(x => string.Equals(x.InternalToken, ScenarioEngineFixture.BotManagerAccessToken, StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task InitialSync_WithInvalidActiveBotItems_Should_NotBreakStartup()
    {
        await using var fixture = await ScenarioEngineFixture.StartIsolatedAsync(botManager =>
        {
            botManager.AddActiveBot(BotPlatformType.Telegram, ScenarioEngineTestData.MalformedBotId());
            botManager.AddActiveBot(BotPlatformType.Telegram, ScenarioEngineTestData.MalformedProjectId());
            botManager.AddActiveBot(BotPlatformType.Telegram, ScenarioEngineTestData.MissingCredentials());
            botManager.AddActiveBot(BotPlatformType.Vk, ScenarioEngineTestData.VkWithNonNumericCommunityId());
        });

        await fixture.BotManager.WaitForGetActiveBotsCallsAsync(2);
        using var ready = await fixture.GetHealthAsync("/health/ready");

        await Assert.That(ready.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task InitialSync_WithValidActiveBot_Should_CacheBot_ForIncomingEvents()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using var fixture = await ScenarioEngineFixture.StartIsolatedAsync(
            botManager => botManager.AddActiveBot(
                BotPlatformType.Telegram,
                ScenarioEngineTestData.TelegramActiveBot(botId, projectId)),
            scenarioRepository => scenarioRepository.AddScenario(
                projectId,
                1,
                ScenarioEngineTestData.MinimalReceiveMessageGraphJson()));

        await fixture.BotManager.WaitForGetActiveBotsCallsAsync(2);
        var beforeRepositoryCalls = fixture.ScenarioRepository.GetScenarioByVersionCallCount;

        await fixture.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);

        await fixture.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 1, beforeRepositoryCalls);
        await fixture.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
    }
}
