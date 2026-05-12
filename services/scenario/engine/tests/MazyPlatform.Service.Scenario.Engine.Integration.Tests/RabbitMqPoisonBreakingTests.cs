namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using Grpc.Core;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Integration.Events;
using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class RabbitMqPoisonBreakingTests : IntegrationTestBase
{
    [Test]
    public async Task BotManagerUnavailableDuringInitialSync_Should_ServiceReady()
    {
        await using var fixture = await ScenarioEngineFixture.StartIsolatedAsync(
            botManager => botManager.SetStreamActiveBotsFailure(StatusCode.Unavailable));

        await fixture.BotManager.WaitForGetActiveBotsCallsAsync(2);
        using var ready = await fixture.GetHealthAsync("/health/ready");
        await Assert.That(ready.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task BurstIncomingForUnknownBot_Should_AllAck_AndNoRepositoryCalls()
    {
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;
        var projectIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();

        foreach (var projectId in projectIds)
            await App.EventPublisher.PublishIncomingAsync(Guid.NewGuid(), projectId: projectId);

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);

        foreach (var projectId in projectIds)
            await Assert.That(App.ScenarioRepository.CountGetScenarioByVersionCalls(projectId, version: 1)).IsEqualTo(0);
    }

    [Test]
    public async Task BurstOfDuplicateDeactivateEvents_Should_AllAck_AndNoDlq()
    {
        var botId = Guid.NewGuid();
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Deactivated))).Ready;

        for (var i = 0; i < 10; i++)
            await App.EventPublisher.PublishDeactivatedAsync(botId);

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Deactivated);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Deactivated), beforeDeadLetters);
    }

    [Test]
    public async Task MalformedKnownEvent_Should_RetryOnce_ThenDlq()
    {
        var deadLetterQueue = ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Deactivated);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(deadLetterQueue)).Ready;

        await App.EventPublisher.PublishRawToExchangeAsync(
            IntegrationEventRoutingKeys.Of<BotInstanceDeactivatedIntegrationEvent>(),
            "{");

        await App.Queues.WaitForReadyMessagesAtLeastAsync(deadLetterQueue, beforeDeadLetters + 1);
    }

    [Test]
    public async Task MalformedRawPayloadInsideIncoming_Should_GoToDlq()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deadLetterQueue = ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(deadLetterQueue)).Ready;
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId, rawPayload: "{}");

        await App.Queues.WaitForReadyMessagesAtLeastAsync(deadLetterQueue, beforeDeadLetters + 1);
    }

    [Test]
    public async Task ScenarioRepositoryUnavailableForOneIncoming_Should_NotPoisonNextIncoming()
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

        App.ScenarioRepository.SetFailure(StatusCode.Unavailable);
        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        App.ScenarioRepository.SetFailure(null);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);

        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 1, beforeRepositoryCalls);
    }

    [Test]
    public async Task UnknownRoutingKey_InExchange_Should_NotCreateDlqMessages()
    {
        var deadLetterQueues = ScenarioEngineQueues.Main.Select(ScenarioEngineQueues.DeadLetter).ToArray();
        var before = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var queue in deadLetterQueues)
            before[queue] = (await App.Queues.GetStatsAsync(queue)).Ready;

        await App.EventPublisher.PublishRawToExchangeAsync("scenario-engine.unknown-routing-key", """{"ignored":true}""");
        await Task.Delay(TimeSpan.FromMilliseconds(750));

        foreach (var queue in deadLetterQueues)
        {
            var stats = await App.Queues.GetStatsAsync(queue);
            await Assert.That(stats.Ready).IsEqualTo(before[queue]);
        }
    }
}
