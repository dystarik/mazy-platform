namespace MazyPlatform.Service.Bot.Manager.Domain.Tests.BotInstances;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.Service.Bot.Manager.Domain.Common;

public class BotInstanceTests
{
    private static readonly Guid OwnerAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 5, 6, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Create_Should_SetScenarioVersionUpdateModeToAuto_ByDefault()
    {
        var bot = CreateBot();

        await Assert.That(bot.ScenarioVersionUpdateMode).IsEqualTo(ScenarioVersionUpdateMode.Auto);
    }

    [Test]
    public async Task ChangeScenarioVersionUpdateMode_Should_UpdateModeAndUpdatedAt()
    {
        var bot = CreateBot();
        var changedAt = Now.AddMinutes(5);

        var result = bot.ChangeScenarioVersionUpdateMode(ScenarioVersionUpdateMode.Manual, changedAt);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(bot.ScenarioVersionUpdateMode).IsEqualTo(ScenarioVersionUpdateMode.Manual);
        await Assert.That(bot.UpdatedAt).IsEqualTo(changedAt);
    }

    [Test]
    public async Task ChangeScenarioVersionUpdateMode_Should_ReturnValidationError_When_ModeInvalid()
    {
        var bot = CreateBot();

        var result = bot.ChangeScenarioVersionUpdateMode((ScenarioVersionUpdateMode)999, Now.AddMinutes(5));

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.BotInstance.InvalidScenarioVersionUpdateMode);
    }

    [Test]
    public async Task ApplyScenarioRelease_Should_ChangeVersionAndAddEvent_When_ModeIsAuto()
    {
        var bot = CreateBot(scenarioVersion: 1);
        var releasedAt = Now.AddMinutes(10);

        var result = bot.ApplyScenarioRelease(2, releasedAt);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsTrue();
        await Assert.That(bot.ScenarioVersion).IsEqualTo(2);
        await Assert.That(bot.UpdatedAt).IsEqualTo(releasedAt);
        await Assert.That(bot.DomainEvents.OfType<BotInstanceScenarioVersionChangedDomainEvent>().Any()).IsTrue();
    }

    [Test]
    public async Task ApplyScenarioRelease_Should_NotChangeVersion_When_ModeIsManual()
    {
        var bot = CreateBot(scenarioVersion: 1);
        var modeResult = bot.ChangeScenarioVersionUpdateMode(ScenarioVersionUpdateMode.Manual, Now.AddMinutes(5));
        var releasedAt = Now.AddMinutes(10);

        var result = bot.ApplyScenarioRelease(2, releasedAt);

        await Assert.That(modeResult.IsSuccess).IsTrue();
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsFalse();
        await Assert.That(bot.ScenarioVersion).IsEqualTo(1);
        await Assert.That(bot.UpdatedAt).IsEqualTo(Now.AddMinutes(5));
        await Assert.That(bot.DomainEvents.OfType<BotInstanceScenarioVersionChangedDomainEvent>().Any()).IsFalse();
    }

    private static BotInstance CreateBot(int scenarioVersion = 1)
    {
        return BotInstance.Create(
            OwnerAccountId,
            ProjectId,
            "Test bot",
            new TelegramBotCredentials("token"),
            scenarioVersion,
            Now);
    }
}
