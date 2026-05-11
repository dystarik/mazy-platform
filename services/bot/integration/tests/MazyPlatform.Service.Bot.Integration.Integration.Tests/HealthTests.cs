namespace MazyPlatform.Service.Bot.Integration.Integration.Tests;

using System.Net;

using MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

public sealed class HealthTests : IntegrationTestBase
{
    [Test]
    public async Task LiveAndReady_Should_ReturnHealthy()
    {
        using var live = await App.GetHealthAsync("/health/live");
        using var ready = await App.GetHealthAsync("/health/ready");

        await Assert.That(live.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(ready.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task InvalidRequiredConfig_Should_NotBecomeReady()
    {
        var invalidBotManager = await App.StartInvalidContainerAsync("BotManager__Address", string.Empty);
        var invalidRabbit = await App.StartInvalidContainerAsync("RabbitMq__Host", string.Empty);
        var invalidVk = await App.StartInvalidContainerAsync("Vk__ApiVersion", string.Empty);
        var invalidTelegram = await App.StartInvalidContainerAsync("Telegram__TimeoutSeconds", string.Empty);

        await Assert.That(invalidBotManager).IsNotNull();
        await Assert.That(invalidRabbit).IsNotNull();
        await Assert.That(invalidVk).IsNotNull();
        await Assert.That(invalidTelegram).IsNotNull();
    }

    [Test]
    public async Task Startup_Should_CreateConfiguredQueuesAndDeadLetterQueues()
    {
        foreach (var queue in BotIntegrationQueues.Main)
        {
            var mainStats = await App.Queues.GetStatsAsync(queue);
            var deadLetterStats = await App.Queues.GetStatsAsync(BotIntegrationQueues.DeadLetter(queue));

            await Assert.That(mainStats.Total).IsGreaterThanOrEqualTo(0);
            await Assert.That(deadLetterStats.Total).IsGreaterThanOrEqualTo(0);
        }
    }
}
