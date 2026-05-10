namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using System.Net;

using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

public sealed class UserAccountEventTests : IntegrationTestBase
{
    [Test]
    public async Task EmailConfirmedEvent_Should_BeIdempotent_AndNotBreakService()
    {
        var userAccountId = Guid.NewGuid();

        await App.EventPublisher.PublishEmailConfirmedAsync(userAccountId);
        await App.EventPublisher.PublishEmailConfirmedAsync(userAccountId);

        var project = await App.Factory.CreateProjectAsync(userAccountId);

        await Assert.That(project.OwnerAccountId).IsEqualTo(userAccountId);
    }

    [Test]
    public async Task UnknownRoutingKey_Should_BeIgnored_AndNotBreakConsumer()
    {
        await App.EventPublisher.PublishUnknownAsync();

        using var response = await App.GetHealthAsync("/health/ready");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task MalformedEmailConfirmedEvent_Should_NotBreakService()
    {
        await App.EventPublisher.PublishMalformedEmailConfirmedAsync();

        using var response = await App.GetHealthAsync("/health/ready");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
