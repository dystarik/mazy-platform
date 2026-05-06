namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserSessions;

using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;

using NSubstitute;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class UserSessionTests
{
    [Test]
    public async Task CreateNewSession_Should_ReturnSession_When_ValidParameters()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var refreshTokenHash = RefreshTokenHash.FromTrusted("hash");
        var now = DateTimeOffset.UtcNow;

        // Act
        var session = UserSession.CreateNewSession(userAccountId, refreshTokenHash, now);

        // Assert
        await Assert.That(session).IsNotNull();
        await Assert.That(session.UserAccountId).IsEqualTo(userAccountId);
        await Assert.That(session.RefreshTokenHash).IsEqualTo(refreshTokenHash);
        await Assert.That(session.ExpiresAt).IsEqualTo(now.AddDays(7));
        await Assert.That(session.CreatedAt).IsEqualTo(now);
        await Assert.That(session.IsRevoked).IsFalse();
        await Assert.That(session.RevokedAt).IsNull();
        await Assert.That(session.RevokedReason).IsNull();
    }

    [Test]
    public async Task CreateNewSession_Should_ThrowArgumentException_When_UserAccountIdIsEmpty()
    {
        // Arrange
        var refreshTokenHash = RefreshTokenHash.FromTrusted("hash");
        var now = DateTimeOffset.UtcNow;

        // Act
        void Action() => UserSession.CreateNewSession(Guid.Empty, refreshTokenHash, now);

        // Assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Test]
    public async Task RefreshSession_Should_ReturnSuccess_When_ValidTokenProvided()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var initialHash = RefreshTokenHash.FromTrusted("hashed_token1");
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.CreateNewSession(userAccountId, initialHash, now);

        var hasher = Substitute.For<ITokenHasher>();
        hasher.Verify(token: "plain_token1", tokenHash: "hashed_token1").Returns(returnThis: true);

        var newHash = RefreshTokenHash.FromTrusted("hashed_token2");
        var refreshTime = now.AddDays(1);

        // Act
        var result = session.RefreshSession(hasher, oldToken: "plain_token1", newHash, refreshTime);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.RefreshTokenHash).IsEqualTo(newHash);
        await Assert.That(session.ExpiresAt).IsEqualTo(refreshTime.AddDays(7));
    }

    [Test]
    public async Task RefreshSession_Should_ReturnError_When_SessionIsRevoked()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var initialHash = RefreshTokenHash.FromTrusted("hashed_token1");
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.CreateNewSession(userAccountId, initialHash, now);

        session.Revoke(RevokedReason.Logout, now);

        var hasher = Substitute.For<ITokenHasher>();
        var newHash = RefreshTokenHash.FromTrusted("hashed_token2");

        // Act
        var result = session.RefreshSession(hasher, oldToken: "plain_token1", newHash, now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task RefreshSession_Should_ReturnError_When_SessionExpired()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var initialHash = RefreshTokenHash.FromTrusted("hashed_token1");
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.CreateNewSession(userAccountId, initialHash, now);

        var hasher = Substitute.For<ITokenHasher>();
        var newHash = RefreshTokenHash.FromTrusted("hashed_token2");

        var expiredTime = now.AddDays(8);

        // Act
        var result = session.RefreshSession(hasher, oldToken: "plain_token1", newHash, expiredTime);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task RefreshSession_Should_ReturnError_When_TokenIsInvalid()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var initialHash = RefreshTokenHash.FromTrusted("hashed_token1");
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.CreateNewSession(userAccountId, initialHash, now);

        var hasher = Substitute.For<ITokenHasher>();
        hasher.Verify(token: "invalid_token", tokenHash: "hashed_token1").Returns(returnThis: false);

        var newHash = RefreshTokenHash.FromTrusted("hashed_token2");

        // Act
        var result = session.RefreshSession(hasher, oldToken: "invalid_token", newHash, now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Revoke_Should_SetRevokedFields_When_Called()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var initialHash = RefreshTokenHash.FromTrusted("hash");
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.CreateNewSession(userAccountId, initialHash, now);

        // Act
        session.Revoke(RevokedReason.Logout, now);

        // Assert
        await Assert.That(session.IsRevoked).IsTrue();
        await Assert.That(session.RevokedAt).IsEqualTo(now);
        await Assert.That(session.RevokedReason).IsEqualTo(RevokedReason.Logout);
    }
}
