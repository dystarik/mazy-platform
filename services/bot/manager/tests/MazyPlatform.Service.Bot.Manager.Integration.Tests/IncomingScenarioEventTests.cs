namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class IncomingScenarioEventTests : IntegrationTestBase
{
    [Test]
    public async Task ProjectDeleted_Should_UnbindProjectBots_AndPublishEvents()
    {
        var projectId = Guid.NewGuid();
        var bot = await App.Factory.CreateTelegramBotAsync(projectId: projectId);
        var before = App.Events.Count;

        await App.EventPublisher.PublishProjectDeletedAsync(projectId);

        var unbound = await App.Events.WaitForBotAsync(RabbitMqEventCapture.UnboundFromProject, bot.BotInstanceId, before);
        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(unbound.FormerProjectId).IsEqualTo(projectId);
        await Assert.That(response.HasProjectId).IsFalse();
        await Assert.That(response.HasScenarioVersion).IsFalse();
    }

    [Test]
    public async Task ReleaseChanged_Should_UpdateAutoBots_ButNotManualBots()
    {
        var owner = BotManagerTestFactory.CreateUserId();
        var projectId = Guid.NewGuid();
        var autoBot = await App.Factory.CreateTelegramBotAsync(owner, projectId, scenarioVersion: 1, BotScenarioVersionUpdateMode.Auto);
        var manualBot = await App.Factory.CreateTelegramBotAsync(owner, projectId, scenarioVersion: 1, BotScenarioVersionUpdateMode.Manual);
        var before = App.Events.Count;

        await App.EventPublisher.PublishReleaseChangedAsync(projectId, 2);

        var changed = await App.Events.WaitForBotAsync(RabbitMqEventCapture.ScenarioVersionChanged, autoBot.BotInstanceId, before);
        var autoResponse = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = autoBot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);
        var manualResponse = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = manualBot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(changed.NewScenarioVersion).IsEqualTo(2);
        await Assert.That(autoResponse.ScenarioVersion).IsEqualTo(2);
        await Assert.That(manualResponse.ScenarioVersion).IsEqualTo(1);
    }

    [Test]
    public async Task ReleaseRemoved_Should_DeactivateActiveBots()
    {
        var projectId = Guid.NewGuid();
        var bot = await App.Factory.CreateActiveBotAsync(projectId: projectId);
        var before = App.Events.Count;

        await App.EventPublisher.PublishReleaseRemovedAsync(projectId);

        var deactivated = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Deactivated, bot.BotInstanceId, before);
        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(deactivated.BotInstanceId).IsEqualTo(bot.BotInstanceId);
        await Assert.That(response.Status).IsEqualTo(BotStatus.Inactive);
    }

    [Test]
    public async Task ScenarioEvents_ForUnknownProject_Should_NotBreakConsumer()
    {
        await App.EventPublisher.PublishProjectDeletedAsync(Guid.NewGuid());
        await App.EventPublisher.PublishReleaseChangedAsync(Guid.NewGuid(), 10);
        await App.EventPublisher.PublishReleaseRemovedAsync(Guid.NewGuid());

        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();

        await Assert.That(bot.BotInstanceId).IsNotEqualTo(Guid.Empty);
    }
}
