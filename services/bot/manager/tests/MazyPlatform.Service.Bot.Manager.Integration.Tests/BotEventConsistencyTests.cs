namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class BotEventConsistencyTests : IntegrationTestBase
{
    [Test]
    public async Task UnbindActiveBot_Should_PublishDeactivatedAndUnboundEvents()
    {
        var bot = await App.Factory.CreateActiveBotAsync();
        var before = App.Events.Count;

        await App.Bots.UnbindBotFromProjectAsync(
            new UnbindBotFromProjectRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var deactivated = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Deactivated, bot.BotInstanceId, before);
        var unbound = await App.Events.WaitForBotAsync(RabbitMqEventCapture.UnboundFromProject, bot.BotInstanceId, before);

        await Assert.That(deactivated.BotInstanceId).IsEqualTo(bot.BotInstanceId);
        await Assert.That(unbound.FormerProjectId).IsEqualTo(bot.ProjectId);
    }

    [Test]
    public async Task ProjectDeleted_ForActiveBot_Should_PublishDeactivatedAndUnboundEvents()
    {
        var projectId = Guid.NewGuid();
        var bot = await App.Factory.CreateActiveBotAsync(projectId: projectId);
        var before = App.Events.Count;

        await App.EventPublisher.PublishProjectDeletedAsync(projectId);

        var deactivated = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Deactivated, bot.BotInstanceId, before);
        var unbound = await App.Events.WaitForBotAsync(RabbitMqEventCapture.UnboundFromProject, bot.BotInstanceId, before);
        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(deactivated.BotInstanceId).IsEqualTo(bot.BotInstanceId);
        await Assert.That(unbound.FormerProjectId).IsEqualTo(projectId);
        await Assert.That(response.Status).IsEqualTo(BotStatus.Inactive);
        await Assert.That(response.HasProjectId).IsFalse();
    }

    [Test]
    public async Task DeleteBot_Should_PublishProjectIdOnlyForBoundBot()
    {
        var bound = await App.Factory.CreateTelegramBotAsync();
        var standalone = await App.Factory.CreateTelegramBotWithoutProjectAsync();
        var beforeBoundDelete = App.Events.Count;

        await App.Bots.DeleteBotAsync(
            new DeleteBotRequest { BotInstanceId = bound.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bound.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var boundDeleted = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Deleted, bound.BotInstanceId, beforeBoundDelete);
        var beforeStandaloneDelete = App.Events.Count;

        await App.Bots.DeleteBotAsync(
            new DeleteBotRequest { BotInstanceId = standalone.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(standalone.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var standaloneDeleted = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Deleted, standalone.BotInstanceId, beforeStandaloneDelete);

        await Assert.That(boundDeleted.ProjectId).IsEqualTo(bound.ProjectId);
        await Assert.That(standaloneDeleted.ProjectId).IsNull();
    }

    [Test]
    public async Task RepeatedFailedOperations_Should_NotPublishEvents()
    {
        var active = await App.Factory.CreateActiveBotAsync();
        var before = App.Events.Count;

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ActivateBotAsync(
            new ActivateBotRequest { BotInstanceId = active.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(active.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.Activated, active.BotInstanceId, before);

        await App.Bots.DeactivateBotAsync(
            new DeactivateBotRequest { BotInstanceId = active.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(active.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        _ = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Deactivated, active.BotInstanceId, before);
        var beforeRepeatedDeactivate = App.Events.Count;

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.DeactivateBotAsync(
            new DeactivateBotRequest { BotInstanceId = active.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(active.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.Deactivated, active.BotInstanceId, beforeRepeatedDeactivate);
    }
}
