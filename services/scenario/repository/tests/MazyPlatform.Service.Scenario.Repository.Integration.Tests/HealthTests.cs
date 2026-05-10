namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using System.Net;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

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
    public async Task RepositoryContainer_WithInvalidRequiredConfiguration_Should_NotBecomeReady()
    {
        var exception = await App.StartInvalidRepositoryContainerAsync();

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task InternalEndpoint_WithoutToken_Should_Fail()
    {
        var project = await App.Factory.CreateReleasedProjectAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Internal.ValidateProjectForBotAsync(new ValidateProjectForBotRequest
        {
            ProjectId = project.ProjectId.ToString(),
            OwnerAccountId = project.OwnerAccountId.ToString(),
            PlatformType = PlatformType.Universal,
            ScenarioVersion = 1,
        }));
    }

    [Test]
    public async Task InternalEndpoint_WithWrongToken_Should_Fail()
    {
        var project = await App.Factory.CreateReleasedProjectAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Internal.ValidateProjectForBotAsync(
            new ValidateProjectForBotRequest
            {
                ProjectId = project.ProjectId.ToString(),
                OwnerAccountId = project.OwnerAccountId.ToString(),
                PlatformType = PlatformType.Universal,
                ScenarioVersion = 1,
            },
            GrpcTestMetadata.ForInternal("wrong-token"),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task InternalEndpoint_WithValidToken_Should_BeAvailable()
    {
        var project = await App.Factory.CreateReleasedProjectAsync();

        await App.Internal.ValidateProjectForBotAsync(
            new ValidateProjectForBotRequest
            {
                ProjectId = project.ProjectId.ToString(),
                OwnerAccountId = project.OwnerAccountId.ToString(),
                PlatformType = PlatformType.Universal,
                ScenarioVersion = 1,
            },
            GrpcTestMetadata.ForInternal(ScenarioRepositoryFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);
    }
}
