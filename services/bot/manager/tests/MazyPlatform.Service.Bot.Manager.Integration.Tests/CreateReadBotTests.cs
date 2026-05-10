namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using Google.Protobuf.WellKnownTypes;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using ScenarioPlatformType = MazyPlatform.Contracts.Scenario.Repository.Grpc.PlatformType;

public sealed class CreateReadBotTests : IntegrationTestBase
{
    [Test]
    public async Task CreateBotWithoutProject_Telegram_Should_CreateInactiveUnboundBot()
    {
        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.BotInstanceId).IsEqualTo(bot.BotInstanceId.ToString());
        await Assert.That(response.PlatformType).IsEqualTo(BotPlatformType.Telegram);
        await Assert.That(response.Status).IsEqualTo(BotStatus.Inactive);
        await Assert.That(response.HasProjectId).IsFalse();
        await Assert.That(response.HasScenarioVersion).IsFalse();
        await Assert.That(response.MaskedAccessToken).IsNotEqualTo(bot.AccessToken);
    }

    [Test]
    public async Task CreateBotWithoutProject_Vk_Should_CreateInactiveBotWithCommunity()
    {
        var bot = await App.Factory.CreateVkBotWithoutProjectAsync();

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.PlatformType).IsEqualTo(BotPlatformType.Vk);
        await Assert.That(response.CommunityId).IsEqualTo(bot.CommunityId);
        await Assert.That(response.Status).IsEqualTo(BotStatus.Inactive);
    }

    [Test]
    public async Task CreateBot_WithValidProjectVersion_Should_CallScenarioRepository_AndCreateBot()
    {
        var before = App.ScenarioRepository.ValidateProjectForBotCallCount;
        var bot = await App.Factory.CreateTelegramBotAsync();

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(App.ScenarioRepository.ValidateProjectForBotCallCount).IsGreaterThan(before);
        await Assert.That(response.ProjectId).IsEqualTo(bot.ProjectId!.Value.ToString());
        await Assert.That(response.ScenarioVersion).IsEqualTo(bot.ScenarioVersion!.Value);
    }

    [Test]
    public async Task CreateBot_WithDefaultScenarioVersion_Should_Fail_AndNotCallScenarioRepository()
    {
        var owner = await App.Factory.CreateUserAsync();
        var projectId = Guid.NewGuid();
        App.Factory.RegisterValidScenarioProject(owner, projectId, ScenarioPlatformType.Telegram, 1);
        var before = App.ScenarioRepository.ValidateProjectForBotCallCount;

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.CreateBotAsync(
            new CreateBotRequest
            {
                ProjectId = projectId.ToString(),
                Name = BotManagerTestFactory.UniqueBotName(),
                PlatformType = BotPlatformType.Telegram,
                AccessToken = BotManagerTestFactory.UniqueTelegramToken(),
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));

        await Assert.That(App.ScenarioRepository.ValidateProjectForBotCallCount).IsEqualTo(before);
    }

    [Test]
    public async Task Lists_Should_ReturnOnlyCurrentOwnerBots()
    {
        var owner = BotManagerTestFactory.CreateUserId();
        var ownBot = await App.Factory.CreateTelegramBotAsync(owner);
        _ = await App.Factory.CreateTelegramBotAsync();

        var byUser = await App.Bots.GetBotsByUserIdAsync(
            new Empty(),
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);
        var byProject = await App.Bots.GetBotsByProjectAsync(
            new GetBotsByProjectRequest { ProjectId = ownBot.ProjectId!.Value.ToString() },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(byUser.Items.Any(i => string.Equals(i.BotInstanceId, ownBot.BotInstanceId.ToString(), StringComparison.Ordinal))).IsTrue();
        await Assert.That(byUser.Items.All(i => string.Equals(i.BotInstanceId, ownBot.BotInstanceId.ToString(), StringComparison.Ordinal))).IsTrue();
        await Assert.That(byProject.Items.Count).IsEqualTo(1);
        await Assert.That(byProject.Items[0].BotInstanceId).IsEqualTo(ownBot.BotInstanceId.ToString());
    }

    [Test]
    public async Task OtherUser_Should_NotSeeForeignBot()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var otherUser = BotManagerTestFactory.CreateUserId();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));

        var response = await App.Bots.GetBotsByProjectAsync(
            new GetBotsByProjectRequest { ProjectId = bot.ProjectId!.Value.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.Items).IsEmpty();
    }

    [Test]
    public async Task PublicMethods_WithMissingMetadata_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.GetBotsByUserIdAsync(
            new Empty(),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task CreateBot_WithUnknownUser_Should_Fail()
    {
        var owner = BotManagerTestFactory.CreateUserId();
        var projectId = Guid.NewGuid();
        App.Factory.RegisterValidScenarioProject(owner, projectId, ScenarioPlatformType.Telegram, 1);

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.CreateBotAsync(
            new CreateBotRequest
            {
                ProjectId = projectId.ToString(),
                Name = BotManagerTestFactory.UniqueBotName(),
                PlatformType = BotPlatformType.Telegram,
                AccessToken = BotManagerTestFactory.UniqueTelegramToken(),
                ScenarioVersion = 1,
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task CreateBot_WithInvalidInputs_Should_Fail()
    {
        var owner = await App.Factory.CreateUserAsync();
        var projectId = Guid.NewGuid();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.CreateBotAsync(
            new CreateBotRequest { ProjectId = "not-a-guid", Name = BotManagerTestFactory.UniqueBotName(), PlatformType = BotPlatformType.Telegram, AccessToken = "token", ScenarioVersion = 1 },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.CreateBotAsync(
            new CreateBotRequest { ProjectId = projectId.ToString(), Name = string.Empty, PlatformType = BotPlatformType.Telegram, AccessToken = "token", ScenarioVersion = 1 },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.CreateBotAsync(
            new CreateBotRequest { ProjectId = projectId.ToString(), Name = BotManagerTestFactory.UniqueBotName(), PlatformType = BotPlatformType.Telegram, AccessToken = string.Empty, ScenarioVersion = 1 },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.CreateBotWithoutProjectAsync(
            new CreateBotWithoutProjectRequest { Name = BotManagerTestFactory.UniqueBotName(), PlatformType = BotPlatformType.Unspecified, AccessToken = "token" },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.CreateBotWithoutProjectAsync(
            new CreateBotWithoutProjectRequest { Name = BotManagerTestFactory.UniqueBotName(), PlatformType = BotPlatformType.Vk, AccessToken = "token" },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
    }
}
