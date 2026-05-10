namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using ScenarioPlatformType = MazyPlatform.Contracts.Scenario.Repository.Grpc.PlatformType;

public sealed class BotStateTests : IntegrationTestBase
{
    [Test]
    public async Task BindBotToProject_Should_BindStandaloneBot_AndCallScenarioRepository()
    {
        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();
        var projectId = Guid.NewGuid();
        App.Factory.RegisterValidScenarioProject(bot.OwnerAccountId, projectId, ScenarioPlatformType.Telegram, 3);
        var before = App.ScenarioRepository.ValidateProjectForBotCallCount;

        await App.Bots.BindBotToProjectAsync(
            new BindBotToProjectRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                ProjectId = projectId.ToString(),
                ScenarioVersion = 3,
                ScenarioVersionUpdateMode = BotScenarioVersionUpdateMode.Manual,
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(App.ScenarioRepository.ValidateProjectForBotCallCount).IsGreaterThan(before);
        await Assert.That(response.ProjectId).IsEqualTo(projectId.ToString());
        await Assert.That(response.ScenarioVersion).IsEqualTo(3);
        await Assert.That(response.ScenarioVersionUpdateMode).IsEqualTo(BotScenarioVersionUpdateMode.Manual);
    }

    [Test]
    public async Task BindBotToProject_WithForeignBotOrInvalidProject_Should_Fail()
    {
        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();
        var otherUser = BotManagerTestFactory.CreateUserId();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.BindBotToProjectAsync(
            new BindBotToProjectRequest { BotInstanceId = bot.BotInstanceId.ToString(), ProjectId = Guid.NewGuid().ToString(), ScenarioVersion = 1 },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.BindBotToProjectAsync(
            new BindBotToProjectRequest { BotInstanceId = bot.BotInstanceId.ToString(), ProjectId = Guid.NewGuid().ToString(), ScenarioVersion = 1 },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task BindBotToProject_WithPlatformMismatch_Should_Fail()
    {
        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();
        var projectId = Guid.NewGuid();
        App.Factory.RegisterValidScenarioProject(bot.OwnerAccountId, projectId, ScenarioPlatformType.Vk, 1);

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.BindBotToProjectAsync(
            new BindBotToProjectRequest { BotInstanceId = bot.BotInstanceId.ToString(), ProjectId = projectId.ToString(), ScenarioVersion = 1 },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task UnbindBotFromProject_Should_ClearProjectVersion_AndPublishEvent()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var before = App.Events.Count;

        await App.Bots.UnbindBotFromProjectAsync(
            new UnbindBotFromProjectRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var unbound = await App.Events.WaitForBotAsync(RabbitMqEventCapture.UnboundFromProject, bot.BotInstanceId, before);
        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(unbound.FormerProjectId).IsEqualTo(bot.ProjectId);
        await Assert.That(response.HasProjectId).IsFalse();
        await Assert.That(response.HasScenarioVersion).IsFalse();
    }

    [Test]
    public async Task UnbindActiveBot_Should_DeactivateIt()
    {
        var bot = await App.Factory.CreateActiveBotAsync();

        await App.Bots.UnbindBotFromProjectAsync(
            new UnbindBotFromProjectRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.Status).IsEqualTo(BotStatus.Inactive);
    }

    [Test]
    public async Task ActivateAndDeactivateBot_Should_ChangeStatus_AndPublishEvents()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var beforeActivate = App.Events.Count;

        await App.Bots.ActivateBotAsync(
            new ActivateBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var activated = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Activated, bot.BotInstanceId, beforeActivate);
        var active = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var beforeDeactivate = App.Events.Count;

        await App.Bots.DeactivateBotAsync(
            new DeactivateBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var deactivated = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Deactivated, bot.BotInstanceId, beforeDeactivate);
        var inactive = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(activated.ProjectId).IsEqualTo(bot.ProjectId);
        await Assert.That(active.Status).IsEqualTo(BotStatus.Active);
        await Assert.That(deactivated.BotInstanceId).IsEqualTo(bot.BotInstanceId);
        await Assert.That(inactive.Status).IsEqualTo(BotStatus.Inactive);
    }

    [Test]
    public async Task ActivateUnboundOrRepeatedStateCalls_Should_Fail()
    {
        var unbound = await App.Factory.CreateTelegramBotWithoutProjectAsync();
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ActivateBotAsync(
            new ActivateBotRequest { BotInstanceId = unbound.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(unbound.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        var active = await App.Factory.CreateActiveBotAsync();
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ActivateBotAsync(
            new ActivateBotRequest { BotInstanceId = active.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(active.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        await App.Bots.DeactivateBotAsync(
            new DeactivateBotRequest { BotInstanceId = active.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(active.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.DeactivateBotAsync(
            new DeactivateBotRequest { BotInstanceId = active.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(active.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }
}
