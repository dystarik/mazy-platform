namespace MazyPlatform.Service.Bot.Integration.Integration.Tests;

using Grpc.Core;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

public sealed class RobustnessTests : IntegrationTestBase
{
    [Test]
    public async Task RepeatedLifecycleEvents_Should_BeIdempotent_AndNotDeadLetter()
    {
        var botId = Guid.NewGuid();

        await PublishTwiceAndAssertNoDeadLetterAsync(BotIntegrationQueues.Deactivated, () => App.EventPublisher.PublishDeactivatedAsync(botId));
        await PublishTwiceAndAssertNoDeadLetterAsync(BotIntegrationQueues.Deleted, () => App.EventPublisher.PublishDeletedAsync(botId));
        await PublishTwiceAndAssertNoDeadLetterAsync(BotIntegrationQueues.Unbound, () => App.EventPublisher.PublishUnboundAsync(botId));
        await PublishTwiceAndAssertNoDeadLetterAsync(BotIntegrationQueues.VersionChanged, () => App.EventPublisher.PublishVersionChangedAsync(botId));
    }

    [Test]
    public async Task Activated_WithMalformedIds_Should_GoToDeadLetter()
    {
        var deadLetterQueue = BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Activated);
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
    public async Task Activated_WhenBotManagerFails_Should_Ack_AndServiceShouldContinue()
    {
        var botId = Guid.NewGuid();
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Activated))).Ready;
        var beforeCalls = App.BotManager.GetBotCredentialsCallCount;
        App.BotManager.SetGetCredentialsFailure(StatusCode.Unavailable);

        try
        {
            await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram);

            await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCalls);
            await App.Queues.WaitForQueueDrainedAsync(BotIntegrationQueues.Activated);
            await App.Queues.AssertNoNewReadyMessagesAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Activated), beforeDeadLetters);
        }
        finally
        {
            App.BotManager.SetGetCredentialsFailure(null);
        }

        await App.EventPublisher.PublishDeactivatedAsync(Guid.NewGuid());
        await App.Queues.WaitForQueueDrainedAsync(BotIntegrationQueues.Deactivated);
    }

    [Test]
    public async Task AfterMalformedEvent_ServiceShould_ProcessValidEventFromAnotherQueue()
    {
        var activatedDeadLetterQueue = BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Activated);
        var beforeActivatedDeadLetters = (await App.Queues.GetStatsAsync(activatedDeadLetterQueue)).Ready;
        var beforeDeactivatedDeadLetters = (await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Deactivated))).Ready;

        await App.EventPublisher.PublishRawToExchangeAsync(
            IntegrationEventRoutingKeys.Of<BotInstanceActivatedIntegrationEvent>(),
            "{");
        await App.Queues.WaitForReadyMessagesAtLeastAsync(activatedDeadLetterQueue, beforeActivatedDeadLetters + 1);

        await App.EventPublisher.PublishDeactivatedAsync(Guid.NewGuid());

        await App.Queues.WaitForQueueDrainedAsync(BotIntegrationQueues.Deactivated);
        await App.Queues.AssertNoNewReadyMessagesAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Deactivated), beforeDeactivatedDeadLetters);
    }

    private static async Task PublishTwiceAndAssertNoDeadLetterAsync(string queueName, Func<Task> publish)
    {
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(queueName))).Ready;

        await publish();
        await publish();

        await App.Queues.WaitForQueueDrainedAsync(queueName);
        await App.Queues.AssertNoNewReadyMessagesAsync(BotIntegrationQueues.DeadLetter(queueName), beforeDeadLetters);
    }
}
