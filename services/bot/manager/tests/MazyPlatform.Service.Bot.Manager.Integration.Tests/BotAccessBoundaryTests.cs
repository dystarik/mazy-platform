namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class BotAccessBoundaryTests : IntegrationTestBase
{
    [Test]
    public async Task GetBotsByProject_ForForeignOwner_Should_ReturnEmptyList()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var otherUser = await App.Factory.CreateUserAsync();

        var response = await App.Bots.GetBotsByProjectAsync(
            new GetBotsByProjectRequest { ProjectId = bot.ProjectId!.Value.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.Items).IsEmpty();
    }

    [Test]
    public async Task HasBotsOnScenarioVersion_Should_NotDependOnOwner()
    {
        var bot = await App.Factory.CreateTelegramBotAsync(scenarioVersion: 7);
        _ = await App.Factory.CreateUserAsync();

        var response = await App.Internal.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest
            {
                ProjectId = bot.ProjectId!.Value.ToString(),
                ScenarioVersion = 7,
            },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.HasBots).IsTrue();
    }

    [Test]
    public async Task ForeignOwnerPublicCalls_Should_NotRevealOrMutateBot()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var otherUser = await App.Factory.CreateUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.UnbindBotFromProjectAsync(
            new UnbindBotFromProjectRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.DeleteBotAsync(
            new DeleteBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));

        var ownerResponse = await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(ownerResponse.ProjectId).IsEqualTo(bot.ProjectId!.Value.ToString());
        await Assert.That(ownerResponse.ScenarioVersion).IsEqualTo(bot.ScenarioVersion!.Value);
        await Assert.That(ownerResponse.MaskedAccessToken).IsNotEqualTo(bot.AccessToken);
    }
}
