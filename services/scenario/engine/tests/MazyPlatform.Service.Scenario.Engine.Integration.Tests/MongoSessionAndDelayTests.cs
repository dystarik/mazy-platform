namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

using MongoDB.Bson;

public sealed class MongoSessionAndDelayTests : IntegrationTestBase
{
    [Test]
    public async Task DelayResume_Disabled_Should_NotClaimDueSessions()
    {
        var botId = Guid.NewGuid();
        const string PlatformUserId = "delay-disabled-user";

        await using var fixture = await ScenarioEngineFixture.StartIsolatedAsync(delayResumeEnabled: false);
        await fixture.InsertDelaySessionAsync(botId, PlatformUserId, DateTimeOffset.UtcNow.AddSeconds(-5));

        await Task.Delay(TimeSpan.FromSeconds(2));

        var session = await fixture.FindSessionAsync(botId, PlatformUserId);
        await Assert.That(session).IsNotNull();
        await Assert.That(session!.Contains("delayResumeLockUntil")).IsFalse();
    }

    [Test]
    public async Task DelayResume_Enabled_Should_CreateIndex_AndClaimDueSession()
    {
        var botId = Guid.NewGuid();
        var platformUserId = $"delay-due-{Guid.NewGuid():N}";

        await Assert.That(await App.HasSessionIndexAsync("ix_sessions_delay_resume")).IsTrue();

        await App.InsertDelaySessionAsync(botId, platformUserId, DateTimeOffset.UtcNow.AddSeconds(-5));

        await App.WaitForLockedSessionAsync(botId, platformUserId);
    }

    [Test]
    public async Task DelayResume_Enabled_Should_NotClaimFutureOrLockedSessions()
    {
        var futureBotId = Guid.NewGuid();
        var lockedBotId = Guid.NewGuid();
        var futureUser = $"delay-future-{Guid.NewGuid():N}";
        var lockedUser = $"delay-locked-{Guid.NewGuid():N}";

        await App.InsertDelaySessionAsync(futureBotId, futureUser, DateTimeOffset.UtcNow.AddMinutes(5));
        await App.InsertDelaySessionAsync(lockedBotId, lockedUser, DateTimeOffset.UtcNow.AddSeconds(-5), lockInFuture: true);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var futureSession = await App.FindSessionAsync(futureBotId, futureUser);
        var lockedSession = await App.FindSessionAsync(lockedBotId, lockedUser);

        await Assert.That(futureSession).IsNotNull();
        await Assert.That(lockedSession).IsNotNull();
        await Assert.That(futureSession!.Contains("delayResumeLockUntil")).IsFalse();
        await Assert.That(lockedSession!.Contains("delayResumeLockedAt")).IsFalse();
    }

    [Test]
    public async Task Incoming_WithValidReceiveMessageGraph_Should_CreateMongoSession()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        const string PlatformUserId = "1001";
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload("prime", platformUserId: 1001));
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload("hello-from-test", platformUserId: 1001, messageId: 3004));
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);

        await App.WaitForSessionAsync(botId, PlatformUserId);
        var session = await App.FindSessionAsync(botId, PlatformUserId);

        await Assert.That(session).IsNotNull();
        await Assert.That(session!["variables"].AsBsonDocument["message_text"].AsString).IsEqualTo("hello-from-test");
        await Assert.That(session["variables"].AsBsonDocument["message_id"].AsString).IsEqualTo("3004");
    }

    [Test]
    public async Task TelegramStartCommand_Should_ResetSession()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var platformUserId = "1002";
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        var startNodeId = Guid.NewGuid();

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson(startNodeId));
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.InsertWaitingSessionAsync(
            botId,
            platformUserId,
            startNodeId,
            new BsonDocument { ["message_text"] = "stale" });

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload("/start", platformUserId: 1002));
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.WaitForSessionWithoutVariableAsync(botId, platformUserId, "message_text");
        var afterStartSession = await App.FindSessionAsync(botId, platformUserId);

        await Assert.That(afterStartSession).IsNotNull();
        await Assert.That(afterStartSession!["variables"].AsBsonDocument.Contains("message_text")).IsFalse();
    }
}
