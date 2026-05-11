namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class GraphExecutionBreakingTests : IntegrationTestBase
{
    [Test]
    public async Task GraphExecutionError_Should_SaveCompletedOrErrorStateAccordingToCurrentBehavior()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170301L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await ActivateTelegramBotAsync(botId, projectId, CreateMissingSchemaGraphJson());

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload(platformUserId: userId));

        await App.WaitForSessionStateAsync(botId, platformUserId, "Completed");
    }

    [Test]
    public async Task GraphWithLoopOverMaxIterations_Should_NotHang()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170302L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming))).Ready;

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.LoopGraphJson());

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload(platformUserId: userId));

        await App.WaitForSessionStateAsync(botId, platformUserId, "Completed");
        await App.Queues.AssertNoNewReadyMessagesAsync(ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming), beforeDeadLetters);
    }

    [Test]
    public async Task GraphWithMissingStartNode_Should_GoToDlq()
    {
        await AssertInvalidGraphGoesToDlqAsync(ScenarioEngineTestData.GraphWithoutNodesJson());
    }

    [Test]
    public async Task GraphWithSetVariableThenReceiveMessage_Should_PersistVariableBeforeWait()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = 170303L;
        var platformUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.SetVariableThenReceiveMessageGraphJson());

        await App.EventPublisher.PublishIncomingAsync(
            botId,
            PlatformType.Telegram,
            projectId,
            rawPayload: ScenarioEngineTestData.TelegramMessagePayload(platformUserId: userId));

        await App.WaitForSessionVariableAsync(botId, platformUserId, "before_wait", "saved");
        await App.WaitForSessionStateAsync(botId, platformUserId, "WaitingForEvent");
    }

    [Test]
    public async Task GraphWithUnknownNodeType_Should_GoToDlq()
    {
        await AssertInvalidGraphGoesToDlqAsync(CreateUnknownNodeGraphJson());
    }

    [Test]
    public async Task RepositoryReturnsDifferentGraphAfterCache_Should_KeepCachedGraph()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;

        await ActivateTelegramBotAsync(botId, projectId, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 1, beforeRepositoryCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);

        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.GraphWithoutNodesJson());
        var callsAfterFirstLoad = App.ScenarioRepository.GetScenarioByVersionCallCount;

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.BotIncoming);

        await Assert.That(App.ScenarioRepository.GetScenarioByVersionCallCount).IsEqualTo(callsAfterFirstLoad);
    }

    [Test]
    public async Task VersionChanged_Should_BypassOldScenarioCache()
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var beforeCredentialsCalls = App.BotManager.GetBotCredentialsCallCount;
        var beforeRepositoryCalls = App.ScenarioRepository.GetScenarioByVersionCallCount;

        App.BotManager.SetCredentials(botId, ScenarioEngineTestData.TelegramCredentials());
        App.ScenarioRepository.AddScenario(projectId, 1, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        App.ScenarioRepository.AddScenario(projectId, 2, ScenarioEngineTestData.MinimalReceiveMessageGraphJson());
        await App.EventPublisher.PublishActivatedAsync(botId, PlatformType.Telegram, projectId, scenarioVersion: 1);
        await App.BotManager.WaitForGetBotCredentialsCallAsync(botId, beforeCredentialsCalls);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.Activated);

        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);
        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 1, beforeRepositoryCalls);
        await App.EventPublisher.PublishVersionChangedAsync(botId, newScenarioVersion: 2);
        await App.Queues.WaitForQueueDrainedAsync(ScenarioEngineQueues.VersionChanged);
        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId, scenarioVersion: 2);

        await App.ScenarioRepository.WaitForGetScenarioByVersionCallAsync(projectId, 2, beforeRepositoryCalls);
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

    private static async Task AssertInvalidGraphGoesToDlqAsync(string graphJson)
    {
        var botId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deadLetterQueue = ScenarioEngineQueues.DeadLetter(ScenarioEngineQueues.BotIncoming);
        var beforeDeadLetters = (await App.Queues.GetStatsAsync(deadLetterQueue)).Ready;

        await ActivateTelegramBotAsync(botId, projectId, graphJson);
        await App.EventPublisher.PublishIncomingAsync(botId, PlatformType.Telegram, projectId);

        await App.Queues.WaitForReadyMessagesAtLeastAsync(deadLetterQueue, beforeDeadLetters + 1);
    }

    private static string CreateMissingSchemaGraphJson()
    {
        var nodeId = Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{nodeId}}",
              "nodes": [
                {
                  "id": "{{nodeId}}",
                  "type": "create_record",
                  "params": {
                    "entityName": "missing_entity",
                    "fields": {},
                    "recordIdVariable": "record_id"
                  }
                }
              ],
              "connections": []
            }
            """;
    }

    private static string CreateUnknownNodeGraphJson()
    {
        var nodeId = Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{nodeId}}",
              "nodes": [
                { "id": "{{nodeId}}", "type": "not_registered_node", "params": {} }
              ],
              "connections": []
            }
            """;
    }
}
