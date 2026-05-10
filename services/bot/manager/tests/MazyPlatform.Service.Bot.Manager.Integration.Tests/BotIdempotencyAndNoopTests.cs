namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class BotIdempotencyAndNoopTests : IntegrationTestBase
{
    [Test]
    public async Task ChangeBotScenarioVersion_WithSameVersion_Should_BeNoop()
    {
        var bot = await App.Factory.CreateTelegramBotAsync(scenarioVersion: 3);
        var before = App.Events.Count;

        await App.Bots.ChangeBotScenarioVersionAsync(
            new ChangeBotScenarioVersionRequest { BotInstanceId = bot.BotInstanceId.ToString(), NewScenarioVersion = 3 },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.ScenarioVersion).IsEqualTo(3);
        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.ScenarioVersionChanged, bot.BotInstanceId, before);
    }

    [Test]
    public async Task ChangeBotScenarioVersionUpdateMode_WithSameMode_Should_BeNoop()
    {
        var bot = await App.Factory.CreateTelegramBotAsync(updateMode: BotScenarioVersionUpdateMode.Manual);

        await App.Bots.ChangeBotScenarioVersionUpdateModeAsync(
            new ChangeBotScenarioVersionUpdateModeRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                ScenarioVersionUpdateMode = BotScenarioVersionUpdateMode.Manual,
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.ScenarioVersionUpdateMode).IsEqualTo(BotScenarioVersionUpdateMode.Manual);
    }

    [Test]
    public async Task ReleaseChanged_WithSameVersion_Should_NotPublishScenarioVersionChanged()
    {
        var projectId = Guid.NewGuid();
        var bot = await App.Factory.CreateTelegramBotAsync(projectId: projectId, scenarioVersion: 2, updateMode: BotScenarioVersionUpdateMode.Auto);
        var before = App.Events.Count;

        await App.EventPublisher.PublishReleaseChangedAsync(projectId, 2);

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.ScenarioVersion).IsEqualTo(2);
        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.ScenarioVersionChanged, bot.BotInstanceId, before);
    }

    [Test]
    public async Task ReleaseRemoved_ForInactiveBot_Should_NotPublishDeactivated()
    {
        var projectId = Guid.NewGuid();
        var bot = await App.Factory.CreateTelegramBotAsync(projectId: projectId);
        var before = App.Events.Count;

        await App.EventPublisher.PublishReleaseRemovedAsync(projectId);

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.Status).IsEqualTo(BotStatus.Inactive);
        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.Deactivated, bot.BotInstanceId, before);
    }
}
