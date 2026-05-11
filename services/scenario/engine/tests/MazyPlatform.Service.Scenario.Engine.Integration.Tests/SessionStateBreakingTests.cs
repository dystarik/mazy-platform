namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

using MongoDB.Bson;

public sealed class SessionStateBreakingTests : IntegrationTestBase
{
    [Test]
    public async Task CompletedSession_Should_CreateNewActiveSessionOnNextIncoming()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170101L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.InsertSessionAsync(botId, platformUserId, "Completed", currentNodeId: null, new BsonDocument { ["stale"] = "yes" });
        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload(platformUserId: userId));

        await App.WaitForSessionStateAsync(botId, platformUserId, "WaitingForEvent");
        var session = await App.FindSessionAsync(botId, platformUserId);
        await Assert.That(session!["variables"].AsBsonDocument.Contains("stale")).IsFalse();
    }

    [Test]
    public async Task EntryPointPayload_Should_ClearPreviousVariables_AndRouteToEntryNode()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170102L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var waitingNodeId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.EntryPointGraphJson(waitingNodeId, payload: "menu"));
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
        await App.InsertWaitingSessionAsync(botId, platformUserId, waitingNodeId, new BsonDocument { ["stale"] = "yes" });

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramCallbackPayload("menu", platformUserId: userId));

        await App.WaitForSessionVariableAsync(botId, platformUserId, "entry_route", "hit");
        var session = await App.FindSessionAsync(botId, platformUserId);
        await Assert.That(session!["variables"].AsBsonDocument.Contains("stale")).IsFalse();
    }

    [Test]
    public async Task ReceiveMessage_FirstIncomingOnlyPrimesSession()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170103L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload("first", platformUserId: userId));

        await App.WaitForSessionStateAsync(botId, platformUserId, "WaitingForEvent");
        var session = await App.FindSessionAsync(botId, platformUserId);
        await Assert.That(session!["variables"].AsBsonDocument.Contains("message_text")).IsFalse();
    }

    [Test]
    public async Task ReceiveMessage_SecondIncomingStoresVariables()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170104L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId, rawPayload: ScenarioEngineTestData.TelegramMessagePayload("prime", platformUserId: userId));
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);
        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId, rawPayload: ScenarioEngineTestData.TelegramMessagePayload("second", platformUserId: userId, messageId: 4004));

        await App.WaitForSessionVariableAsync(botId, platformUserId, "message_text", "second");
        await App.WaitForSessionVariableAsync(botId, platformUserId, "message_id", "4004");
    }

    [Test]
    public async Task TelegramStartCommand_ButNotPrefix_Should_NotReset()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170105L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var waitingNodeId = Guid.NewGuid();

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.MinimalReceiveMessageGraphJson(waitingNodeId));
        await App.InsertWaitingSessionAsync(botId, platformUserId, waitingNodeId, new BsonDocument { ["message_text"] = "keep" });

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload("/starter", platformUserId: userId));

        await App.WaitForSessionVariableAsync(botId, platformUserId, "message_text", "/starter");
    }

    [Test]
    public async Task TelegramStartCommand_Should_ClearUserVariables_ButKeepSystemVariables()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170106L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var waitingNodeId = Guid.NewGuid();

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.MinimalReceiveMessageGraphJson(waitingNodeId));
        await App.InsertWaitingSessionAsync(botId, platformUserId, waitingNodeId, new BsonDocument { ["message_text"] = "stale" });

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload("/start", platformUserId: userId));

        await App.WaitForSessionWithoutVariableAsync(botId, platformUserId, "message_text");
        await App.WaitForSessionVariableAsync(botId, platformUserId, "_userId", platformUserId);
        await App.WaitForSessionVariableAsync(botId, platformUserId, "_scenarioVersion", "1");
    }

    [Test]
    public async Task TelegramStartCommand_WithBotUsername_Should_Reset()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170107L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var waitingNodeId = Guid.NewGuid();

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.MinimalReceiveMessageGraphJson(waitingNodeId));
        await App.InsertWaitingSessionAsync(botId, platformUserId, waitingNodeId, new BsonDocument { ["message_text"] = "stale" });

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload("/start@SomeBot", platformUserId: userId));

        await App.WaitForSessionWithoutVariableAsync(botId, platformUserId, "message_text");
    }

    [Test]
    public async Task VkStartCommand_Should_ClearUserVariables()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170108L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var waitingNodeId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.VkCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson(waitingNodeId));
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Vk, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
        await App.InsertWaitingSessionAsync(botId, platformUserId, waitingNodeId, new BsonDocument { ["message_text"] = "stale" });

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Vk,
            projectId,
            rawPayload: ScenarioEngineTestData.VkMessagePayload("начать", platformUserId: userId));

        await App.WaitForSessionWithoutVariableAsync(botId, platformUserId, "message_text");
    }

    [Test]
    public async Task WaitingSession_WithPlatformMismatchIncoming_Should_NotMutateVariables()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170109L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var waitingNodeId = Guid.NewGuid();

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.MinimalReceiveMessageGraphJson(waitingNodeId));
        await App.InsertWaitingSessionAsync(botId, platformUserId, waitingNodeId, new BsonDocument { ["message_text"] = "keep" });

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Vk,
            projectId,
            rawPayload: ScenarioEngineTestData.VkMessagePayload("mutate", platformUserId: userId));
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);

        await App.WaitForSessionVariableAsync(botId, platformUserId, "message_text", "keep");
    }

    [Test]
    public async Task WaitingSession_WithUnknownCurrentNode_Should_EndControlled_AndNotCrashService()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170110L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.InsertWaitingSessionAsync(botId, platformUserId, Guid.NewGuid(), new BsonDocument { ["message_text"] = "stale" });

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload("next", platformUserId: userId));

        await App.WaitForSessionStateAsync(botId, platformUserId, "Completed");
    }

    private static async Task ActivateTelegramBotAsync(Guid botId, Guid projectId, string graphJson)
    {
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, graphJson);
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);
    }
}
