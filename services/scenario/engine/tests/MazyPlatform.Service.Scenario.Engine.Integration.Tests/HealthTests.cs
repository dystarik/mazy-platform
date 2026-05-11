namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using System.Net;

using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed class HealthTests : IntegrationTestBase
{
    [Test]
    public async Task InvalidRequiredConfig_Should_NotBecomeReady()
    {
        var invalidRabbit = await App.StartInvalidContainerAsync("RabbitMq__Host", string.Empty);
        var invalidMongoConnection = await App.StartInvalidContainerAsync("MongoDb__ConnectionString", string.Empty);
        var invalidMongoDatabase = await App.StartInvalidContainerAsync("MongoDb__DatabaseName", string.Empty);
        var invalidBotManagerAddress = await App.StartInvalidContainerAsync("BotManager__Address", string.Empty);
        var invalidBotManagerToken = await App.StartInvalidContainerAsync("BotManager__AccessToken", string.Empty);
        var invalidScenarioRepository = await App.StartInvalidContainerAsync("ScenarioRepository__Address", string.Empty);
        var invalidVk = await App.StartInvalidContainerAsync("Vk__ApiVersion", string.Empty);

        await Assert.That(invalidRabbit).IsNotNull();
        await Assert.That(invalidMongoConnection).IsNotNull();
        await Assert.That(invalidMongoDatabase).IsNotNull();
        await Assert.That(invalidBotManagerAddress).IsNotNull();
        await Assert.That(invalidBotManagerToken).IsNotNull();
        await Assert.That(invalidScenarioRepository).IsNotNull();
        await Assert.That(invalidVk).IsNotNull();
    }

    [Test]
    public async Task LiveAndReady_Should_ReturnHealthy()
    {
        using var live = await App.GetHealthAsync("/health/live");
        using var ready = await App.GetHealthAsync("/health/ready");

        await Assert.That(live.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(ready.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Startup_Should_CreateConfiguredQueuesAndDeadLetterQueues()
    {
        foreach (var queue in ScenarioEngineQueues.Main)
        {
            var mainStats = await App.Queues.GetStatsAsync(queue);
            var deadLetterStats = await App.Queues.GetStatsAsync(ScenarioEngineQueues.DeadLetter(queue));

            await Assert.That(mainStats.Total).IsGreaterThanOrEqualTo(0);
            await Assert.That(deadLetterStats.Total).IsGreaterThanOrEqualTo(0);
        }
    }
}
