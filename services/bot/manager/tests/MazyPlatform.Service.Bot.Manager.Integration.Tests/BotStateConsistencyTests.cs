namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using ScenarioPlatformType = MazyPlatform.Contracts.Scenario.Repository.Grpc.PlatformType;

public sealed class BotStateConsistencyTests : IntegrationTestBase
{
    [Test]
    public async Task FailedBindBotToProject_Should_NotChangeExistingBinding()
    {
        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();
        var missingProjectId = Guid.NewGuid();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.BindBotToProjectAsync(
            new BindBotToProjectRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                ProjectId = missingProjectId.ToString(),
                ScenarioVersion = 1,
                ScenarioVersionUpdateMode = BotScenarioVersionUpdateMode.Manual,
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.HasProjectId).IsFalse();
        await Assert.That(response.HasScenarioVersion).IsFalse();
        await Assert.That(response.ScenarioVersionUpdateMode).IsEqualTo(BotScenarioVersionUpdateMode.Auto);
    }

    [Test]
    public async Task FailedChangeBotScenarioVersion_Should_KeepCurrentVersion()
    {
        var bot = await App.Factory.CreateTelegramBotAsync(scenarioVersion: 1);

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ChangeBotScenarioVersionAsync(
            new ChangeBotScenarioVersionRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                NewScenarioVersion = 99,
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.ProjectId).IsEqualTo(bot.ProjectId!.Value.ToString());
        await Assert.That(response.ScenarioVersion).IsEqualTo(1);
    }

    [Test]
    public async Task FailedVkTokenUpdate_Should_KeepPreviousCredentials()
    {
        var bot = await App.Factory.CreateVkBotAsync();
        var before = App.Events.Count;

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.UpdateBotTokenAsync(
            new UpdateBotTokenRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                AccessToken = BotManagerTestFactory.UniqueVkToken(),
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        var publicBot = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var credentials = await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(publicBot.CommunityId).IsEqualTo(bot.CommunityId);
        await Assert.That(credentials.Credentials.Vk.AccessToken).IsEqualTo(bot.AccessToken);
        await Assert.That(credentials.Credentials.Vk.CommunityId).IsEqualTo(bot.CommunityId);
        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.TokenChanged, bot.BotInstanceId, before);
    }

    [Test]
    public async Task ForeignOwnerMutatingCalls_Should_NotChangeBotState()
    {
        var bot = await App.Factory.CreateTelegramBotAsync(scenarioVersion: 1);
        var otherUser = await App.Factory.CreateUserAsync();
        var before = App.Events.Count;

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ActivateBotAsync(
            new ActivateBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.UpdateBotTokenAsync(
            new UpdateBotTokenRequest { BotInstanceId = bot.BotInstanceId.ToString(), AccessToken = BotManagerTestFactory.UniqueTelegramToken() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ChangeBotScenarioVersionAsync(
            new ChangeBotScenarioVersionRequest { BotInstanceId = bot.BotInstanceId.ToString(), NewScenarioVersion = 2 },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));

        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var credentials = await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.Status).IsEqualTo(BotStatus.Inactive);
        await Assert.That(response.ScenarioVersion).IsEqualTo(1);
        await Assert.That(credentials.Credentials.Telegram.AccessToken).IsEqualTo(bot.AccessToken);
        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.Activated, bot.BotInstanceId, before);
        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.TokenChanged, bot.BotInstanceId, before);
        await App.Events.AssertNoBotEventAsync(RabbitMqEventCapture.ScenarioVersionChanged, bot.BotInstanceId, before);
    }
}
