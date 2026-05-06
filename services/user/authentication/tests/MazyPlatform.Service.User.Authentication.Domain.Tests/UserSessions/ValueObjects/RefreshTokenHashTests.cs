namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserSessions.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class RefreshTokenHashTests
{
    [Test]
    public async Task Create_Should_ReturnSuccess_When_HashIsValid()
    {
        // Arrange
        const string validHash = "a1b2c3d4e5f6"; // Hex

        // Act
        var result = RefreshTokenHash.Create(validHash);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Value).IsEqualTo(validHash);
    }

    [Test]
    public async Task Create_Should_ReturnError_When_HashIsInvalid()
    {
        // Arrange
        const string invalidHash = "invalid_hash_%^";

        // Act
        var result = RefreshTokenHash.Create(invalidHash);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task FromTrusted_Should_CreateRefreshTokenHash_When_Called()
    {
        // Arrange
        const string trustedHash = "trusted_value_123";

        // Act
        var result = RefreshTokenHash.FromTrusted(trustedHash);

        // Assert
        await Assert.That(result.Value).IsEqualTo(trustedHash);
    }
}
