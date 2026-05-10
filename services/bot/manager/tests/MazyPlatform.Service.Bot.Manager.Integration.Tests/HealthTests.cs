namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed class HealthTests : IntegrationTestBase
{
    [Test]
    public async Task LiveAndReady_Should_ReturnHealthy()
    {
        using var live = await App.GetHealthAsync("/health/live");
        using var ready = await App.GetHealthAsync("/health/ready");

        await Assert.That(live.IsSuccessStatusCode).IsTrue();
        await Assert.That(ready.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task InvalidRequiredConfig_Should_NotBecomeReady()
    {
        var exception = await App.StartInvalidBotManagerContainerAsync();

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task InternalGrpc_Should_RequireInternalToken()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = Guid.NewGuid().ToString() },
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = Guid.NewGuid().ToString() },
            GrpcTestMetadata.ForInternal("wrong-token"),
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Internal.GetBotCredentialsAsync(
            new GetBotCredentialsRequest { BotInstanceId = Guid.NewGuid().ToString() },
            GrpcTestMetadata.ForInternal(BotManagerFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline));
    }
}
