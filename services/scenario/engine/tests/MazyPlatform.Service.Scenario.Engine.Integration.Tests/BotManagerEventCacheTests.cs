namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class BotManagerEventCacheTests : IntegrationTestBase
{
    [Test]
    public async Task Activated_WithCredentials_Should_CacheBot_AndAllowIncomingScenarioLoad()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());

        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);

        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 1, beforeRepositoryCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
    }

    [Test]
    public async Task Activated_WithCredentialsNotFound_Should_Ack_AndNotCacheBot()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Activated))).Ready;

        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Activated), beforeDeadLetters);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(beforeRepositoryCalls);
    }

    [Test]
    public async Task Activated_WithUnsupportedPlatform_Should_Ack_AndNotCallBotManager()
    {
        var beforeCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Activated))).Ready;

        await App.EventPublisher.PublishActivatedAsync(Guid.NewGuid(), (PlatformType)999);

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Activated), beforeDeadLetters);
        await Assert.That(App.BotManager.GetBotCredentialsCallCount).IsEqualTo(beforeCalls);
    }

    [Test]
    public async Task RemoveEvents_Should_RemoveBotFromCache()
    {
        await AssertRemoveEventRemovesFromCacheAsync(ScenarioEngineQueues.Deactivated, botId => App.EventPublisher.PublishDeactivatedAsync(botId));
        await AssertRemoveEventRemovesFromCacheAsync(ScenarioEngineQueues.Deleted, botId => App.EventPublisher.PublishDeletedAsync(botId));
        await AssertRemoveEventRemovesFromCacheAsync(ScenarioEngineQueues.Unbound, botId => App.EventPublisher.PublishUnboundAsync(botId));
    }

    [Test]
    public async Task TokenChanged_ForCachedBot_Should_RefreshCredentials()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials("old-token"));
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials("new-token"));
        await App.EventPublisher.PublishTokenChangedAsync(botId);

        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCalls + 1);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.TokenChanged);
    }

    [Test]
    public async Task TokenChanged_ForUnknownBot_Should_Ack_AndNotCallBotManager()
    {
        var beforeCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.TokenChanged))).Ready;

        await App.EventPublisher.PublishTokenChangedAsync(Guid.NewGuid());

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.TokenChanged);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.TokenChanged), beforeDeadLetters);
        await Assert.That(App.BotManager.GetBotCredentialsCallCount).IsEqualTo(beforeCalls);
    }

    [Test]
    public async Task VersionChanged_ForCachedBot_Should_UseNewVersionOnNextIncomingEvent()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 2, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId, scenarioVersion: 1);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishVersionChangedAsync(botId, newScenarioVersion: 2);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.VersionChanged);

        await PublishIncomingUntilScenarioVersionIsRequestedAsync(botId, projectId, version: 2, beforeRepositoryCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
    }

    [Test]
    public async Task VersionChanged_ForUnknownBot_Should_Ack_AndNotDeadLetter()
    {
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.VersionChanged))).Ready;

        await App.EventPublisher.PublishVersionChangedAsync(Guid.NewGuid());

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.VersionChanged);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.VersionChanged), beforeDeadLetters);
    }

    private static async Task AssertRemoveEventRemovesFromCacheAsync(string queueName, Func<Guid, Task> publishRemove)
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await publishRemove(botId);
        await App.Queues.WaitForQueueDrainedAsync(queueName);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(beforeRepositoryCalls);
    }

    private static async Task PublishIncomingUntilScenarioVersionIsRequestedAsync(
        Guid botId,
        Guid projectId,
        int version,
        int afterRepositoryCallCount)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId, scenarioVersion: version);
            await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);

            if (App.ScenarioRepository.HasGetScenarioByVersionCall(projectId, version, afterRepositoryCallCount))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"Expected scenario version '{version}' to be requested for project '{projectId}'.");
    }
}
