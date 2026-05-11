namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Integration.Events;
using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class IncomingBotEventTests : IntegrationTestBase
{
    [Test]
    public async Task Incoming_WithMissingScenario_Should_Ack_AndNotDeadLetter()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);
    }

    [Test]
    public async Task Incoming_WithPlatformMismatch_Should_Ack_AndNotCallRepository()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Vk,
            projectId,
            rawPayload: ScenarioEngineTestData.VkMessagePayload());

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);
        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(beforeRepositoryCalls);
    }

    [Test]
    public async Task Incoming_WithUnsupportedPlatform_Should_Ack_AndNotCallRepository()
    {
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;

        await App.EventPublisher.PublishIncomingAsync(Guid.NewGuid(), (PlatformType)999);

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);
        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(beforeRepositoryCalls);
    }

    [Test]
    public async Task Incoming_WithUnknownBot_Should_Ack_AndNotCallRepository()
    {
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;

        await App.EventPublisher.PublishIncomingAsync(Guid.NewGuid());

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);
        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(beforeRepositoryCalls);
    }

    [Test]
    public async Task Incoming_WithValidGraph_Should_LoadScenarioOnce_AndReuseCache()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 1, beforeRepositoryCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);

        var callsAfterFirstIncoming = App.ScenarioRepository.GetScenarioByVersionCallCount;
        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);

        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(callsAfterFirstIncoming);
    }

    [Test]
    public async Task MalformedJson_ForKnownRoutingKey_Should_GoToDeadLetter()
    {
        var deadLetterQueue = ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(deadLetterQueue)).Ready;

        await App.EventPublisher.PublishRawToExchangeAsync(
            IntegrationEventRoutingKeys.Of<BotIncomingEventIntegrationEvent>(),
            "{");

        await App.Queues.WaitForReadyMessagesAtLeastAsync(deadLetterQueue, beforeDeadLetters + 1);
    }

    [Test]
    public async Task UnknownRoutingKeyMessage_Should_Ack_AndNotBreakConsumer()
    {
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Deactivated))).Ready;

        await App.EventPublisher.PublishRawToQueueAsync(ScenarioEngineQueues.Deactivated, """{"ignored":true}""");

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Deactivated);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Deactivated), beforeDeadLetters);

        await App.EventPublisher.PublishDeactivatedAsync(Guid.NewGuid());
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Deactivated);
    }
}
