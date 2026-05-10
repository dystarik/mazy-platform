namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using Google.Protobuf.WellKnownTypes;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class BotCredentialsSafetyTests : IntegrationTestBase
{
    [Test]
    public async Task PublicReadModels_Should_NeverExposeFullAccessToken()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();

        var byId = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var byUser = await App.Bots.GetBotsByUserIdAsync(
            new Empty(),
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var byProject = await App.Bots.GetBotsByProjectAsync(
            new GetBotsByProjectRequest { ProjectId = bot.ProjectId!.Value.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var byUserItem = byUser.Items.Single(x => string.Equals(x.BotInstanceId, bot.BotInstanceId.ToString(), StringComparison.Ordinal));
        var byProjectItem = byProject.Items.Single(x => string.Equals(x.BotInstanceId, bot.BotInstanceId.ToString(), StringComparison.Ordinal));

        await Assert.That(byId.MaskedAccessToken).IsNotEqualTo(bot.AccessToken);
        await Assert.That(byUserItem.MaskedAccessToken).IsNotEqualTo(bot.AccessToken);
        await Assert.That(byProjectItem.MaskedAccessToken).IsNotEqualTo(bot.AccessToken);
    }

    [Test]
    public async Task UpdateBotToken_ForActiveBot_Should_KeepActiveStatus_AndUpdateInternalStreams()
    {
        var bot = await App.Factory.CreateActiveBotAsync();
        var newToken = BotManagerTestFactory.UniqueTelegramToken();

        await App.Bots.UpdateBotTokenAsync(
            new UpdateBotTokenRequest { BotInstanceId = bot.BotInstanceId.ToString(), AccessToken = newToken },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var publicBot = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var activeBots = await ReadActiveBotsAsync(BotPlatformType.Telegram);
        var activeBot = activeBots.Single(x => string.Equals(x.BotInstanceId, bot.BotInstanceId.ToString(), StringComparison.Ordinal));

        await Assert.That(publicBot.Status).IsEqualTo(BotStatus.Active);
        await Assert.That(activeBot.Credentials.Telegram.AccessToken).IsEqualTo(newToken);
    }

    [Test]
    public async Task TelegramTokenUpdate_WithCommunityId_Should_NotStoreCommunityId()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var newToken = BotManagerTestFactory.UniqueTelegramToken();

        await App.Bots.UpdateBotTokenAsync(
            new UpdateBotTokenRequest
            {
                BotInstanceId = bot.BotInstanceId.ToString(),
                AccessToken = newToken,
                CommunityId = "unexpected-community",
            },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var publicBot = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var credentials = await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(publicBot.HasCommunityId).IsFalse();
        await Assert.That(credentials.Credentials.PlatformCase).IsEqualTo(BotCredentials.PlatformOneofCase.Telegram);
        await Assert.That(credentials.Credentials.Telegram.AccessToken).IsEqualTo(newToken);
    }

    private static async Task<List<ActiveBotInfo>> ReadActiveBotsAsync(BotPlatformType platformType)
    {
        using var call = App.Internal.GetActiveBots(
            new GetActiveBotsRequest { PlatformType = platformType },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        var items = new List<ActiveBotInfo>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            items.Add(call.ResponseStream.Current);
        }

        return items;
    }
}
