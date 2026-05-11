namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using Google.Protobuf.WellKnownTypes;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

public sealed class SessionTests : IntegrationTestBase
{
    [Test]
    public async Task RefreshSession_WithValidRefreshToken_Should_RotateTokens()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        var refreshed = await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
            RefreshToken = user.RefreshToken,
        });

        await Assert.That(refreshed.Tokens.AccessToken).IsNotEmpty();
        await Assert.That(refreshed.Tokens.RefreshToken).IsNotEmpty();
        await Assert.That(refreshed.Tokens.RefreshToken).IsNotEqualTo(user.RefreshToken);
    }

    [Test]
    public async Task RefreshSession_WithOldRefreshTokenAfterRotation_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        _ = await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
            RefreshToken = user.RefreshToken,
        });

        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
            RefreshToken = user.RefreshToken,
        }));
    }

    [Test]
    public async Task RefreshSession_WithWrongToken_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
            RefreshToken = Guid.NewGuid().ToString(),
        }));
    }

    [Test]
    public async Task RefreshSession_WithWrongTokenId_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = Guid.NewGuid().ToString(),
            RefreshToken = user.RefreshToken,
        }));
    }

    [Test]
    public async Task RefreshSession_WithMalformedTokenId_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = "not-a-guid",
            RefreshToken = user.RefreshToken,
        }));
    }

    [Test]
    public async Task GetSessions_Should_ReturnCurrentSession()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        var sessions = await App.UserSession.GetSessionsAsync(new Empty(), GrpcTestMetadata.ForUser(user));

        await Assert.That(sessions.Sessions).IsNotEmpty();
        await Assert.That(sessions.Sessions.Any(s =>
            string.Equals(s.RefreshTokenId, user.RefreshTokenId.ToString(), StringComparison.Ordinal) && s.IsCurrent)).IsTrue();
    }

    [Test]
    public async Task Logout_Should_RevokeSession()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await App.UserSession.LogoutAsync(new LogoutRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
        });

        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
            RefreshToken = user.RefreshToken,
        }));
    }

    [Test]
    public async Task Logout_WithAlreadyRevokedSession_Should_BeIdempotent()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await App.UserSession.LogoutAsync(new LogoutRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
        });

        await App.UserSession.LogoutAsync(new LogoutRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
        });

        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = user.RefreshTokenId.ToString(),
            RefreshToken = user.RefreshToken,
        }));
    }

    [Test]
    public async Task Logout_WithMalformedRefreshTokenId_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.LogoutAsync(new LogoutRequest
        {
            RefreshTokenId = "not-a-guid",
        }));
    }

    [Test]
    public async Task LogoutAll_WithExcludeCurrentFalse_Should_RevokeAllSessions()
    {
        var (first, second) = await App.Users.CreateUserWithTwoSessionsAsync();

        await App.UserSession.LogoutAllAsync(
            new LogoutAllRequest { ExcludeCurrentSession = false },
            GrpcTestMetadata.ForUser(second));

        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = first.RefreshTokenId.ToString(),
            RefreshToken = first.RefreshToken,
        }));
        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = second.RefreshTokenId.ToString(),
            RefreshToken = second.RefreshToken,
        }));
    }

    [Test]
    public async Task LogoutAll_WithExcludeCurrentTrue_Should_KeepCurrentSessionOnly()
    {
        var (first, second) = await App.Users.CreateUserWithTwoSessionsAsync();

        await App.UserSession.LogoutAllAsync(
            new LogoutAllRequest { ExcludeCurrentSession = true },
            GrpcTestMetadata.ForUser(second));

        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = first.RefreshTokenId.ToString(),
            RefreshToken = first.RefreshToken,
        }));

        var refreshCurrent = await App.UserSession.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshTokenId = second.RefreshTokenId.ToString(),
            RefreshToken = second.RefreshToken,
        });
        await Assert.That(refreshCurrent.Tokens.AccessToken).IsNotEmpty();
    }

    [Test]
    public async Task AuthenticatedCalls_WithoutMetadata_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.UserSession.GetSessionsAsync(new Empty()));
    }
}
