namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class DelayResumeBreakingTests : IntegrationTestBase
{
    [Test]
    public async Task DelayResume_BatchSize_Should_ClaimOnlyConfiguredCount()
    {
        var firstBotId = Guid.NewGuid();
        var secondBotId = Guid.NewGuid();
        var firstUser = $"batch-one-{Guid.NewGuid():N}";
        var secondUser = $"batch-two-{Guid.NewGuid():N}";

        await using var fixture = await ScenarioEngineFixture.StartIsolatedAsync(delayResumeBatchSize: 1);
        await fixture.InsertDelaySessionAsync(firstBotId, firstUser, DateTimeOffset.UtcNow.AddSeconds(-5));
        await fixture.InsertDelaySessionAsync(secondBotId, secondUser, DateTimeOffset.UtcNow.AddSeconds(-5));
        await fixture.WaitForLockedSessionAsync(firstBotId, firstUser);

        await Assert.That(await fixture.CountLockedSessionsAsync(firstUser, secondUser)).IsEqualTo(1);
    }

    [Test]
    public async Task DelayResume_DueSession_WithCachedBotAndScenario_Should_ResumeAndUpdateSession()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var delayNodeId = Guid.NewGuid();
        var platformUserId = $"delay-ok-{Guid.NewGuid():N}";
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.DelayThenSetVariableGraphJson(delayNodeId));
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
        await App.InsertDelaySessionAsync(botId, platformUserId, delayNodeId, DateTimeOffset.UtcNow.AddSeconds(-5));

        await App.WaitForSessionVariableAsync(botId, platformUserId, "resumed", "yes");
    }

    [Test]
    public async Task DelayResume_DueSession_WithMissingScenario_Should_StayLockedButNotCrash()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var platformUserId = $"delay-missing-scenario-{Guid.NewGuid():N}";
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
        await App.InsertDelaySessionAsync(botId, platformUserId, DateTimeOffset.UtcNow.AddSeconds(-5));

        await App.WaitForLockedSessionAsync(botId, platformUserId);
        using var ready = await App.GetHealthAsync("/health/ready");
        await Assert.That(ready.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task DelayResume_DueSession_WithUnknownBot_Should_StayLockedButNotCrash()
    {
        var botId = Guid.NewGuid();
        var platformUserId = $"delay-unknown-bot-{Guid.NewGuid():N}";

        await App.InsertDelaySessionAsync(botId, platformUserId, DateTimeOffset.UtcNow.AddSeconds(-5));

        await App.WaitForLockedSessionAsync(botId, platformUserId);
        using var ready = await App.GetHealthAsync("/health/ready");
        await Assert.That(ready.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task DelayResume_ExpiredLock_Should_BeClaimedAgain()
    {
        var botId = Guid.NewGuid();
        var platformUserId = $"delay-expired-lock-{Guid.NewGuid():N}";

        await App.InsertDelaySessionWithLockAsync(
            botId,
            platformUserId,
            DateTimeOffset.UtcNow.AddSeconds(-5),
            DateTime.UtcNow.AddSeconds(-5));

        await App.WaitForLockedSessionAsync(botId, platformUserId);
    }

    [Test]
    public async Task DelayResume_InvalidCurrentNode_Should_NotCrashService()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var platformUserId = $"delay-invalid-node-{Guid.NewGuid():N}";
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
        await App.InsertDelaySessionAsync(botId, platformUserId, Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-5));

        await App.WaitForSessionAsync(botId, platformUserId);
        await Task.Delay(TimeSpan.FromSeconds(2));

        using var ready = await App.GetHealthAsync("/health/ready");
        await Assert.That(ready.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task DelayResume_MalformedResumeAt_Should_NotBeClaimed()
    {
        var botId = Guid.NewGuid();
        var platformUserId = $"delay-malformed-{Guid.NewGuid():N}";

        await App.InsertMalformedDelaySessionAsync(botId, platformUserId, "not-an-iso-date");
        await Task.Delay(TimeSpan.FromSeconds(2));

        var session = await App.FindSessionAsync(botId, platformUserId);
        await Assert.That(session).IsNotNull();
        await Assert.That(session!.Contains("delayResumeLockUntil")).IsFalse();
    }
}
