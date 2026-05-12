namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using Grpc.Core;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class CacheConsistencyBreakingTests : IntegrationTestBase
{
    [Test]
    public async Task ActivatedAfterIncoming_Should_AllowNextIncoming()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(beforeRepositoryCalls);

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
    public async Task ActivatedTwice_WithDifferentProject_Should_ReplaceCache()
    {
        var botId = Guid.NewGuid();
        var firstProjectId = Guid.NewGuid();
        var secondProjectId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(firstProjectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        App.ScenarioRepository.AddScenario(secondProjectId, 2, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());

        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, firstProjectId, scenarioVersion: 1);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, secondProjectId, scenarioVersion: 2);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls + 1);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, secondProjectId, scenarioVersion: 2);

        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(secondProjectId, 2, beforeRepositoryCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await Assert.That(App.ScenarioRepository.CountGetScenarioByVersionCalls(firstProjectId, 1, beforeRepositoryCalls)).IsEqualTo(0);
    }

    [Test]
    public async Task DeactivateThenTokenChanged_Should_NotReAddBot()
    {
        await AssertRemoveThenFollowUpDoesNotReAddBotAsync(
            remove: botId => App.EventPublisher.PublishDeactivatedAsync(botId),
            followUp: botId => App.EventPublisher.PublishTokenChangedAsync(botId),
            removeQueue: ScenarioEngineQueues.Deactivated,
            followUpQueue: ScenarioEngineQueues.TokenChanged);
    }

    [Test]
    public async Task DeleteThenVersionChanged_Should_NotReAddBot()
    {
        await AssertRemoveThenFollowUpDoesNotReAddBotAsync(
            remove: botId => App.EventPublisher.PublishDeletedAsync(botId),
            followUp: botId => App.EventPublisher.PublishVersionChangedAsync(botId, newScenarioVersion: 2),
            removeQueue: ScenarioEngineQueues.Deleted,
            followUpQueue: ScenarioEngineQueues.VersionChanged);
    }

    [Test]
    public async Task Incoming_BeforeActivated_Should_Ack_AndNotCacheScenario()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        const string PlatformUserId = "170001";
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload(platformUserId: 170001));

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);
        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(beforeRepositoryCalls);
        await Assert.That(await App.FindSessionAsync(botId, PlatformUserId)).IsNull();
    }

    [Test]
    public async Task ScenarioCache_Should_BeSeparatedByPlatform()
    {
        var telegramBotId = Guid.NewGuid();
        var vkBotId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;

        App.BotManager.SetCredentials(telegramBotId, ScenarioEngineTestData.TelegramCredentials());
        App.BotManager.SetCredentials(vkBotId, ScenarioEngineTestData.VkCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());

        await App.EventPublisher.PublishActivatedAsync(telegramBotId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(telegramBotId, beforeCredentialsCalls);
        await App.EventPublisher.PublishActivatedAsync(vkBotId, PlatformType.Vk, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(vkBotId, beforeCredentialsCalls + 1);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(telegramBotId, PlatformType.Telegram, projectId);
        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 1, beforeRepositoryCalls);
        await App.EventPublisher.PublishIncomingAsync(vkBotId, PlatformType.Vk, projectId, rawPayload: ScenarioEngineTestData.VkMessagePayload());

        await WaitForScenarioCallsAsync(projectId, 1, beforeRepositoryCalls, expectedCount: 2);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
    }

    [Test]
    public async Task ScenarioVersionChanged_ToMissingVersion_Should_Ack_AndIncomingDoesNotDeadLetter()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishVersionChangedAsync(botId, newScenarioVersion: 404);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.VersionChanged);
        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId, scenarioVersion: 404);

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);
    }

    [Test]
    public async Task TokenChanged_WhenCredentialsBecomeNotFound_Should_FixCurrentBehavior_KeepOldCache()
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

        App.BotManager.SetGetCredentialsFailure(StatusCode.NotFound);
        try
        {
            await App.EventPublisher.PublishTokenChangedAsync(botId);
            await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls + 1);
            await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.TokenChanged);
        }
        finally
        {
            App.BotManager.SetGetCredentialsFailure(null);
        }

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);

        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 1, beforeRepositoryCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
    }

    [Test]
    public async Task UnboundThenIncoming_Should_NotLoadScenario()
    {
        await AssertRemoveThenFollowUpDoesNotReAddBotAsync(
            remove: botId => App.EventPublisher.PublishUnboundAsync(botId),
            followUp: botId => App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, Guid.NewGuid()),
            removeQueue: ScenarioEngineQueues.Unbound,
            followUpQueue: ScenarioEngineQueues.BotIncoming);
    }

    private static async Task AssertRemoveThenFollowUpDoesNotReAddBotAsync(
        Func<Guid, Task> remove,
        Func<Guid, Task> followUp,
        string removeQueue,
        string followUpQueue)
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await remove(botId);
        await App.Queues.WaitForQueueDrainedAsync(removeQueue);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        await followUp(botId);
        await App.Queues.WaitForQueueDrainedAsync(followUpQueue);

        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(beforeRepositoryCalls);
    }

    private static async Task WaitForScenarioCallsAsync(Guid projectId, int version, int afterCount, int expectedCount)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            if (App.ScenarioRepository.CountGetScenarioByVersionCalls(projectId, version, afterCount) >= expectedCount)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException($"Expected at least {expectedCount} scenario calls for project '{projectId}' version '{version}'.");
    }
}
