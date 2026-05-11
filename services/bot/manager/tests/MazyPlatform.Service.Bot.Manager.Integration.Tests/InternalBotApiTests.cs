namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using Grpc.Core;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class InternalBotApiTests : IntegrationTestBase
{
    [Test]
    public async Task GetBotCredentials_Should_ReturnFullToken()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();

        var response = await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.Credentials.PlatformType).IsEqualTo(BotPlatformType.Telegram);
        await Assert.That(response.Credentials.Telegram.AccessToken).IsEqualTo(bot.AccessToken);
    }

    [Test]
    public async Task GetBotCredentials_ForMissingBot_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = Guid.NewGuid().ToString() },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task GetActiveBots_Should_StreamOnlyActiveBotsForRequestedPlatform()
    {
        var telegram = await App.Factory.CreateActiveBotAsync();
        var vk = await App.Factory.CreateVkBotAsync();
        await App.Bots.ActivateBotAsync(
            new ActivateBotRequest { BotInstanceId = vk.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(vk.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        _ = await App.Factory.CreateTelegramBotAsync();
        _ = await App.Factory.CreateTelegramBotWithoutProjectAsync();

        var telegramBots = await ReadActiveBotsAsync(BotPlatformType.Telegram);
        var vkBots = await ReadActiveBotsAsync(BotPlatformType.Vk);

        await Assert.That(telegramBots.Any(x => string.Equals(x.BotInstanceId, telegram.BotInstanceId.ToString(), StringComparison.Ordinal))).IsTrue();
        await Assert.That(telegramBots.Any(x => string.Equals(x.BotInstanceId, vk.BotInstanceId.ToString(), StringComparison.Ordinal))).IsFalse();
        await Assert.That(vkBots.Any(x => string.Equals(x.BotInstanceId, vk.BotInstanceId.ToString(), StringComparison.Ordinal))).IsTrue();
        await Assert.That(vkBots.Any(x => string.Equals(x.BotInstanceId, telegram.BotInstanceId.ToString(), StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task HasBotsOnScenarioVersion_Should_ReturnExpectedResult()
    {
        var bot = await App.Factory.CreateTelegramBotAsync(scenarioVersion: 4);

        var sameVersion = await App.Internal.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest { ProjectId = bot.ProjectId!.Value.ToString(), ScenarioVersion = 4 },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);
        var otherVersion = await App.Internal.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest { ProjectId = bot.ProjectId.Value.ToString(), ScenarioVersion = 5 },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);
        var otherProject = await App.Internal.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest { ProjectId = Guid.NewGuid().ToString(), ScenarioVersion = 4 },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(sameVersion.HasBots).IsTrue();
        await Assert.That(otherVersion.HasBots).IsFalse();
        await Assert.That(otherProject.HasBots).IsFalse();
    }

    [Test]
    public async Task InternalCalls_WithMissingOrWrongToken_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Internal.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest { ProjectId = Guid.NewGuid().ToString(), ScenarioVersion = 1 },
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Internal.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest { ProjectId = Guid.NewGuid().ToString(), ScenarioVersion = 1 },
            GrpcTestMetadata.ForInternal("wrong-token"),
            deadline: GrpcTestCall.Deadline));
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
