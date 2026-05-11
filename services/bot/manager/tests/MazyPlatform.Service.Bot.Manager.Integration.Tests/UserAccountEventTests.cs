namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class UserAccountEventTests : IntegrationTestBase
{
    [Test]
    public async Task EmailConfirmed_Should_CreateLocalUserAccount_AndAllowBotCreation()
    {
        var owner = await App.Factory.CreateUserAsync();

        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync(owner);

        await Assert.That(bot.BotInstanceId).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task EmailConfirmed_Should_BeIdempotent()
    {
        var owner = BotManagerTestFactory.CreateUserId();

        await App.EventPublisher.PublishEmailConfirmedAsync(owner);
        await App.EventPublisher.PublishEmailConfirmedAsync(owner);

        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync(owner);

        await Assert.That(bot.OwnerAccountId).IsEqualTo(owner);
    }

    [Test]
    public async Task MalformedAndUnknownEvents_Should_NotBreakConsumer()
    {
        await App.EventPublisher.PublishMalformedEmailConfirmedAsync();
        await App.EventPublisher.PublishUnknownAsync();

        var bot = await App.Factory.CreateTelegramBotWithoutProjectAsync();

        await Assert.That(bot.BotInstanceId).IsNotEqualTo(Guid.Empty);
    }
}
