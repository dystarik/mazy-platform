namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class TokenScenarioTests : IntegrationTestBase
{
    [Test]
    public async Task UpdateBotToken_Should_ChangeCredentials_AndPublishEvent()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var newToken = BotManagerTestFactory.UniqueTelegramToken();
        var before = App.Events.Count;

        await App.Bots.UpdateBotTokenAsync(
            new UpdateBotTokenRequest { BotInstanceId = bot.BotInstanceId.ToString(), AccessToken = newToken },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var tokenChanged = await App.Events.WaitForBotAsync(RabbitMqEventCapture.TokenChanged, bot.BotInstanceId, before);
        var publicBot = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var credentials = await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(tokenChanged.BotInstanceId).IsEqualTo(bot.BotInstanceId);
        await Assert.That(publicBot.MaskedAccessToken).IsNotEqualTo(newToken);
        await Assert.That(credentials.Credentials.Telegram.AccessToken).IsEqualTo(newToken);
    }

    [Test]
    public async Task UpdateBotToken_ForVk_Should_RequireCommunityId_AndRejectForeignBot()
    {
        var bot = await App.Factory.CreateVkBotAsync();
        var otherUser = BotManagerTestFactory.CreateUserId();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.UpdateBotTokenAsync(
            new UpdateBotTokenRequest { BotInstanceId = bot.BotInstanceId.ToString(), AccessToken = BotManagerTestFactory.UniqueVkToken() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.UpdateBotTokenAsync(
            new UpdateBotTokenRequest { BotInstanceId = bot.BotInstanceId.ToString(), AccessToken = BotManagerTestFactory.UniqueVkToken(), CommunityId = "new-community" },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task ChangeBotScenarioVersion_Should_ValidateAndPublishEvent()
    {
        var bot = await App.Factory.CreateTelegramBotAsync(scenarioVersion: 1);
        var before = App.Events.Count;

        await App.Bots.ChangeBotScenarioVersionAsync(
            new ChangeBotScenarioVersionRequest { BotInstanceId = bot.BotInstanceId.ToString(), NewScenarioVersion = 2 },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var changed = await App.Events.WaitForBotAsync(RabbitMqEventCapture.ScenarioVersionChanged, bot.BotInstanceId, before);
        var response = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(changed.NewScenarioVersion).IsEqualTo(2);
        await Assert.That(response.ScenarioVersion).IsEqualTo(2);
    }

    [Test]
    public async Task ChangeBotScenarioVersion_WithUnboundOrMissingVersion_Should_Fail()
    {
        var unbound = await App.Factory.CreateTelegramBotWithoutProjectAsync();
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ChangeBotScenarioVersionAsync(
            new ChangeBotScenarioVersionRequest { BotInstanceId = unbound.BotInstanceId.ToString(), NewScenarioVersion = 2 },
            GrpcTestMetadata.ForUser(unbound.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));

        var bound = await App.Factory.CreateTelegramBotAsync(scenarioVersion: 1);
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.ChangeBotScenarioVersionAsync(
            new ChangeBotScenarioVersionRequest { BotInstanceId = bound.BotInstanceId.ToString(), NewScenarioVersion = 99 },
            GrpcTestMetadata.ForUser(bound.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task ChangeBotScenarioVersionUpdateMode_Should_ChangeMode()
    {
        var bot = await App.Factory.CreateTelegramBotAsync(updateMode: BotScenarioVersionUpdateMode.Auto);

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
}
