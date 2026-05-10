namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

public sealed class ExternalProviderTests : IntegrationTestBase
{
    [Test]
    public async Task LoginByExternalProvider_WithInvalidCode_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByExternalProviderAsync(
            new LoginByExternalProviderRequest
            {
                Provider = ExternalProvider.Yandex,
                Code = "invalid-provider-code",
            },
            deadline: DateTime.UtcNow.AddSeconds(5)));
    }

    [Test]
    public async Task LinkProvider_WithInvalidCode_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.LinkedProvider.LinkProviderAsync(
            new LinkProviderRequest
            {
                Provider = ExternalProvider.Yandex,
                Code = "invalid-provider-code",
            },
            GrpcTestMetadata.ForUser(user),
            deadline: DateTime.UtcNow.AddSeconds(5)));
    }
}
