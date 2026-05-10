namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

public sealed class LoginTests : IntegrationTestBase
{
    [Test]
    public async Task LoginByPassword_WithConfirmedUser_Should_ReturnTokens()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        var response = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        });

        await Assert.That(response.ResultCase).IsEqualTo(LoginByPasswordResponse.ResultOneofCase.Tokens);
        await Assert.That(response.Tokens.AccessToken).IsNotEmpty();
        await Assert.That(response.Tokens.RefreshToken).IsNotEmpty();
    }

    [Test]
    public async Task LoginByPassword_WithPendingUser_Should_Fail()
    {
        var pending = await App.Users.CreatePendingUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = pending.Email,
            Password = pending.Password,
        }));
    }

    [Test]
    public async Task LoginByPassword_WithWrongPassword_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = "WrongPassword123!",
        }));
    }

    [Test]
    public async Task LoginByPassword_WithUnknownEmail_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = TestUserFactory.UniqueEmail(),
            Password = TestUserFactory.DefaultPassword,
        }));
    }

    [Test]
    public async Task LoginByPassword_WithInvalidEmail_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = "bad-email",
            Password = TestUserFactory.DefaultPassword,
        }));
    }

    [Test]
    public async Task LoginByPassword_WithEmptyPassword_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = string.Empty,
        }));
    }

    [Test]
    public async Task LoginByPassword_WithoutMfa_Should_NotPublishEvents()
    {
        var user = await App.Users.CreateConfirmedUserAsync();
        var before = App.Events.Count;

        _ = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        });

        var hasEvent = await App.Events.HasAnyEventAfterAsync(before, TimeSpan.FromSeconds(1));
        await Assert.That(hasEvent).IsFalse();
    }
}
