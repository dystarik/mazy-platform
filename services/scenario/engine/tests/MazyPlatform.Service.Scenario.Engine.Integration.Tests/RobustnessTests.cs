namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using Grpc.Core;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Integration.Events;
using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class RobustnessTests : IntegrationTestBase
{
    [Test]
    public async Task AfterMalformedEvent_ServiceShould_ProcessValidEventFromAnotherQueue()
    {
        var activatedDeadLetterQueue = ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Activated);
        var beforeActivatedDeadLetters = (await App.Queues.GetStatsAsync(activatedDeadLetterQueue)).Ready;
        var beforeDeactivatedDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Deactivated))).Ready;

        await App.EventPublisher.PublishRawToExchangeAsync(
            IntegrationEventRoutingKeys.Of<BotInstanceActivatedIntegrationEvent>(),
            "{");
        await App.Queues.WaitForReadyMessagesAtLeastAsync(activatedDeadLetterQueue, beforeActivatedDeadLetters + 1);

        await App.EventPublisher.PublishDeactivatedAsync(Guid.NewGuid());

        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Deactivated);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Deactivated), beforeDeactivatedDeadLetters);
    }

    [Test]
    public async Task Incoming_WhenScenarioRepositoryFails_Should_Ack_AndServiceShouldContinue()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
        App.ScenarioRepository.SetFailure(StatusCode.Unavailable);

        try
        {
            await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);

            await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
            await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);
        }
        finally
        {
            App.ScenarioRepository.SetFailure(null);
        }

        await App.EventPublisher.PublishDeactivatedAsync(Guid.NewGuid());
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Deactivated);
    }

    [Test]
    public async Task InvalidGraphJson_FromRepository_Should_GoToDeadLetter()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deadLetterQueue = ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(deadLetterQueue)).Ready;
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, "{}");
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);

        await App.Queues.WaitForReadyMessagesAtLeastAsync(deadLetterQueue, beforeDeadLetters + 1);
    }

    [Test]
    public async Task ManagerEvents_WhenBotManagerFails_Should_Ack_AndServiceShouldContinue()
    {
        var botId = Guid.NewGuid();
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Activated))).Ready;
        var beforeCalls = App.BotManager.GetBotCredentialsCallCount;
        App.BotManager.SetGetCredentialsFailure(StatusCode.Unavailable);

        try
        {
            await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram);

            await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCalls);
            await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
            await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Activated), beforeDeadLetters);
        }
        finally
        {
            App.BotManager.SetGetCredentialsFailure(null);
        }

        await App.EventPublisher.PublishDeactivatedAsync(Guid.NewGuid());
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Deactivated);
    }

    [Test]
    public async Task MalformedActivatedEvent_Should_GoToDeadLetter()
    {
        var deadLetterQueue = ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.Activated);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(deadLetterQueue)).Ready;
        const string MalformedActivatedEvent = """
            {
              "OccurredAt": "2026-01-01T00:00:00Z",
              "BotInstanceId": "not-a-guid",
              "ProjectId": "not-a-guid",
              "PlatformType": "Telegram",
              "ScenarioVersion": 1
            }
            """;

        await App.EventPublisher.PublishRawToExchangeAsync(
            IntegrationEventRoutingKeys.Of<BotInstanceActivatedIntegrationEvent>(),
            MalformedActivatedEvent);

        await App.Queues.WaitForReadyMessagesAtLeastAsync(deadLetterQueue, beforeDeadLetters + 1);
    }

    [Test]
    public async Task MalformedIncomingEvent_Should_GoToDeadLetter()
    {
        var deadLetterQueue = ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(deadLetterQueue)).Ready;
        const string MalformedIncomingEvent = """
            {
              "OccurredAt": "2026-01-01T00:00:00Z",
              "Platform": "Telegram",
              "BotId": "not-a-guid",
              "ProjectId": "not-a-guid",
              "ScenarioVersion": 1,
              "RawPayload": "{}"
            }
            """;

        await App.EventPublisher.PublishRawToExchangeAsync(
            IntegrationEventRoutingKeys.Of<BotIncomingEventIntegrationEvent>(),
            MalformedIncomingEvent);

        await App.Queues.WaitForReadyMessagesAtLeastAsync(deadLetterQueue, beforeDeadLetters + 1);
    }

    [Test]
    public async Task RepeatedRemoveEvents_Should_BeIdempotent_AndNotDeadLetter()
    {
        var botId = Guid.NewGuid();

        await PublishTwiceAndAssertNoDeadLetterAsync(ScenarioEngineQueues.Deactivated, () => App.EventPublisher.PublishDeactivatedAsync(botId));
        await PublishTwiceAndAssertNoDeadLetterAsync(ScenarioEngineQueues.Deleted, () => App.EventPublisher.PublishDeletedAsync(botId));
        await PublishTwiceAndAssertNoDeadLetterAsync(ScenarioEngineQueues.Unbound, () => App.EventPublisher.PublishUnboundAsync(botId));
    }

    private static async Task PublishTwiceAndAssertNoDeadLetterAsync(string queueName, Func<Task> publish)
    {
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(queueName))).Ready;

        await publish();
        await publish();

        await App.Queues.WaitForQueueDrainedAsync(queueName);
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(queueName), beforeDeadLetters);
    }
}
