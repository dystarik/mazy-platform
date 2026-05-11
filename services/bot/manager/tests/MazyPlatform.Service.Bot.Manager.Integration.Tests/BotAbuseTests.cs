namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using Grpc.Core;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using ScenarioPlatformType = MazyPlatform.Contracts.Scenario.Repository.Grpc.PlatformType;

public sealed class BotAbuseTests : IntegrationTestBase
{
    [Test]
    public async Task BindBotToProject_WhenAlreadyBound_Should_Fail_AndKeepOriginalBinding()
    {
        var owner = BotManagerTestFactory.CreateUserId();
        var originalProjectId = Guid.NewGuid();
        var newProjectId = Guid.NewGuid();
        var bot = await App.Factory.CreateTelegramBotAsync(owner, originalProjectId, scenarioVersion: 1);
        App.Factory.RegisterValidScenarioProject(owner, newProjectId, ScenarioPlatformType.Telegram, 2);

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.BindBotToProjectAsync(
            new BindBotToProjectRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                ProjectId = newProjectId.ToString(),
                ScenarioVersion = 2,
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.ProjectId).IsEqualTo(originalProjectId.ToString());
        await Assert.That(response.ScenarioVersion).IsEqualTo(1);
    }

    [Test]
    public async Task UnbindBotFromProject_WhenAlreadyUnbound_Should_Fail()
    {
        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.UnbindBotFromProjectAsync(
            new UnbindBotFromProjectRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task BindBotToProject_WithInvalidScenarioVersionOrUpdateMode_Should_Fail()
    {
        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();
        var projectId = Guid.NewGuid();
        App.Factory.RegisterValidScenarioProject(bot.OwnerAccountId, projectId, ScenarioPlatformType.Telegram, 1);

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.BindBotToProjectAsync(
            new BindBotToProjectRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                ProjectId = projectId.ToString(),
                ScenarioVersion = 0,
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.BindBotToProjectAsync(
            new BindBotToProjectRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                ProjectId = projectId.ToString(),
                ScenarioVersion = 1,
                ScenarioVersionUpdateMode = (BotScenarioVersionUpdateMode)999,
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task ChangeBotScenarioVersionUpdateMode_WithInvalidMode_Should_Fail()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ChangeBotScenarioVersionUpdateModeAsync(
            new ChangeBotScenarioVersionUpdateModeRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                ScenarioVersionUpdateMode = (BotScenarioVersionUpdateMode)999,
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task MutatingCommands_WithMalformedBotId_Should_Fail()
    {
        var owner = await App.Factory.CreateUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ActivateBotAsync(
            new ActivateBotRequest { BotInstanceId = "not-a-guid" },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.DeactivateBotAsync(
            new DeactivateBotRequest { BotInstanceId = "not-a-guid" },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.UnbindBotFromProjectAsync(
            new UnbindBotFromProjectRequest { BotInstanceId = "not-a-guid" },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.DeleteBotAsync(
            new DeleteBotRequest { BotInstanceId = "not-a-guid" },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.UpdateBotTokenAsync(
            new UpdateBotTokenRequest { BotInstanceId = "not-a-guid", AccessToken = "token" },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ChangeBotScenarioVersionAsync(
            new ChangeBotScenarioVersionRequest { BotInstanceId = "not-a-guid", NewScenarioVersion = 1 },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ChangeBotScenarioVersionUpdateModeAsync(
            new ChangeBotScenarioVersionUpdateModeRequest { BotInstanceId = "not-a-guid", ScenarioVersionUpdateMode = BotScenarioVersionUpdateMode.Auto },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task PublicQueries_WithMalformedIdsOrBadUserMetadata_Should_Fail()
    {
        var badUser = new Metadata { { "x-user-id", "not-a-guid" }, { "x-trace-id", $"it-{Guid.NewGuid():N}" } };

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = "not-a-guid" },
            GrpcTestMetadata.ForUser(BotManagerTestFactory.CreateUserId()),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.GetBotsByProjectAsync(
            new GetBotsByProjectRequest { ProjectId = "not-a-guid" },
            GrpcTestMetadata.ForUser(BotManagerTestFactory.CreateUserId()),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.GetBotsByUserIdAsync(
            new Google.Protobuf.WellKnownTypes.Empty(),
            badUser,
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task InternalQueries_WithMalformedIdsOrInvalidVersion_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = "not-a-guid" },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Internal.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest { ProjectId = "not-a-guid", ScenarioVersion = 1 },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Internal.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest { ProjectId = Guid.NewGuid().ToString(), ScenarioVersion = 0 },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task TelegramBot_WithCommunityId_Should_NotExposeCommunityId()
    {
        var owner = await App.Factory.CreateUserAsync();
        var response = await App.Bots.CreateBotWithoutProjectAsync(
            new CreateBotWithoutProjectRequest
            {
                Name = BotManagerTestFactory.UniqueBotName(),
                PlatformType = BotPlatformType.Telegram,
                AccessToken = BotManagerTestFactory.UniqueTelegramToken(),
                CommunityId = "unexpected-community",
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);

        var bot = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = response.BotInstanceId },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);
        var credentials = await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = response.BotInstanceId },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(bot.HasCommunityId).IsFalse();
        await Assert.That(credentials.Credentials.PlatformCase).IsEqualTo(BotCredentials.PlatformOneofCase.Telegram);
    }
}
