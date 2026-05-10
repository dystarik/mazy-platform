namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using System.Net;

public sealed class HealthTests : IntegrationTestBase
{
    [Test]
    public async Task LiveHealth_Should_ReturnHealthy()
    {
        using var response = await App.GetHealthAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).Contains("Healthy");
    }

    [Test]
    public async Task ReadyHealth_Should_ReturnHealthy()
    {
        using var response = await App.GetHealthAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).Contains("Healthy");
    }

    [Test]
    public async Task AuthContainer_WithInvalidRequiredConfiguration_Should_NotStartSuccessfully()
    {
        var exception = await App.StartInvalidAuthContainerAsync();

        await Assert.That(exception).IsNotNull();
    }
}
