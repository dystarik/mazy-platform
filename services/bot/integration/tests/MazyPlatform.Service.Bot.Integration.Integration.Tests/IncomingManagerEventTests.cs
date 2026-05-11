namespace MazyPlatform.Service.Bot.Integration.Integration.Tests;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

public sealed class IncomingManagerEventTests : IntegrationTestBase
{
    [Test]
    public async Task UnknownBotLifecycleEvents_Should_Ack_AndNotDeadLetter()
    {
        await AssertAckWithoutDeadLetterAsync(
            BotIntegrationQueues.Deactivated,
            () => App.EventPublisher.PublishDeactivatedAsync(Guid.NewGuid()));
        await AssertAckWithoutDeadLetterAsync(
            BotIntegrationQueues.Deleted,
            () => App.EventPublisher.PublishDeletedAsync(Guid.NewGuid()));
        await AssertAckWithoutDeadLetterAsync(
            BotIntegrationQueues.Unbound,
            () => App.EventPublisher.PublishUnboundAsync(Guid.NewGuid()));
        await AssertAckWithoutDeadLetterAsync(
            BotIntegrationQueues.VersionChanged,
            () => App.EventPublisher.PublishVersionChangedAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task TokenChanged_ForUnknownBot_Should_Ack_AndNotCallBotManager()
    {
        var beforeCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.TokenChanged))).Ready;

        await App.EventPublisher.PublishTokenChangedAsync(Guid.NewGuid());

        await App.Queues.WaitForQueueDrainedAsync(BotIntegrationQueues.TokenChanged);
        await App.Queues.AssertNoNewReadyMessagesAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.TokenChanged), beforeDeadLetters);
        await Assert.That(App.BotManager.GetBotCredentialsCallCount).IsEqualTo(beforeCalls);
    }

    [Test]
    public async Task Activated_WithUnsupportedPlatform_Should_Ack_AndNotCallBotManager()
    {
        var beforeCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Activated))).Ready;

        await App.EventPublisher.PublishActivatedAsync(Guid.NewGuid(), (PlatformType)999);

        await App.Queues.WaitForQueueDrainedAsync(BotIntegrationQueues.Activated);
        await App.Queues.AssertNoNewReadyMessagesAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Activated), beforeDeadLetters);
        await Assert.That(App.BotManager.GetBotCredentialsCallCount).IsEqualTo(beforeCalls);
    }

    [Test]
    public async Task Activated_WithTelegramOrVk_Should_CallGetBotCredentials_AndAckWhenNotFound()
    {
        var telegramBotId = Guid.NewGuid();
        var vkBotId = Guid.NewGuid();
        var beforeCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Activated))).Ready;

        await App.EventPublisher.PublishActivatedAsync(telegramBotId, PlatformType.Telegram);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(telegramBotId, beforeCalls);
        await App.Queues.WaitForQueueDrainedAsync(BotIntegrationQueues.Activated);

        await App.EventPublisher.PublishActivatedAsync(vkBotId, PlatformType.Vk);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(vkBotId, beforeCalls + 1);
        await App.Queues.WaitForQueueDrainedAsync(BotIntegrationQueues.Activated);

        await App.Queues.AssertNoNewReadyMessagesAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Activated), beforeDeadLetters);
        await Assert.That(App.BotManager.GetBotCredentialsCalls.Skip(beforeCalls).All(x =>
            string.Equals(x.InternalToken, BotIntegrationFixture.BotManagerAccessToken, StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task UnknownRoutingKeyMessage_Should_Ack_AndNotBreakConsumer()
    {
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Deactivated))).Ready;

        await App.EventPublisher.PublishRawToQueueAsync(BotIntegrationQueues.Deactivated, """{"ignored":true}""");

        await App.Queues.WaitForQueueDrainedAsync(BotIntegrationQueues.Deactivated);
        await App.Queues.AssertNoNewReadyMessagesAsync(BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Deactivated), beforeDeadLetters);
    }

    [Test]
    public async Task MalformedJson_ForKnownRoutingKey_Should_GoToDeadLetter()
    {
        var deadLetterQueue = BotIntegrationQueues.DeadLetter(BotIntegrationQueues.Deactivated);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(deadLetterQueue)).Ready;

        await App.EventPublisher.PublishRawToExchangeAsync(
            IntegrationEventRoutingKeys.Of<BotInstanceDeactivatedIntegrationEvent>(),
            "{");

        await App.Queues.WaitForReadyMessagesAtLeastAsync(deadLetterQueue, beforeDeadLetters + 1);
    }

    private static async Task AssertAckWithoutDeadLetterAsync(string queueName, Func<Task> publish)
    {
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(queueName))).Ready;

        await publish();

        await App.Queues.WaitForQueueDrainedAsync(queueName);
        await App.Queues.AssertNoNewReadyMessagesAsync(BotIntegrationQueues.DeadLetter(queueName), beforeDeadLetters);
    }
}
