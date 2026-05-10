namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class DeleteBotTests : IntegrationTestBase
{
    [Test]
    public async Task DeleteBot_Should_DeleteBot_AndPublishEvent()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var before = App.Events.Count;

        await App.Bots.DeleteBotAsync(
            new DeleteBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var deleted = await App.Events.WaitForBotAsync(RabbitMqEventCapture.Deleted, bot.BotInstanceId, before);

        await Assert.That(deleted.ProjectId).IsEqualTo(bot.ProjectId);
        await GrpcAssert.ThrowsAsync(async () => await App.Bots.GetBotAsync(
            new GetBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task DeleteBot_WithRepeatedOrForeignCall_Should_Fail()
    {
        var bot = await App.Factory.CreateTelegramBotAsync();
        var otherUser = BotManagerTestFactory.CreateUserId();

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.DeleteBotAsync(
            new DeleteBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));

        await App.Bots.DeleteBotAsync(
            new DeleteBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.Bots.DeleteBotAsync(
            new DeleteBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }
}
