namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserAccounts.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class PasswordHashTests
{
    [Test]
    public async Task Create_Should_ReturnSuccess_When_HashIsValidHex()
    {
        // Arrange
        const string validHexHash = "a1b2c3d4e5f6"; // 12 chars

        // Act
        var result = PasswordHash.Create(validHexHash);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Value).IsEqualTo(validHexHash);
    }

    [Test]
    public async Task Create_Should_ReturnSuccess_When_HashIsValidBase64()
    {
        // Arrange
        const string validBase64Hash = "SGVsbG8gV29ybGQh"; // 16 chars

        // Act
        var result = PasswordHash.Create(validBase64Hash);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Value).IsEqualTo(validBase64Hash);
    }

    [Test]
    public async Task Create_Should_ReturnSuccess_When_HashIsValidBcrypt()
    {
        // Arrange
        const string validBcryptHash = "$2a$12$R.Sj9uO.y5iE2J5O/O4uOu/K1r.mZ0iZ";

        // Act
        var result = PasswordHash.Create(validBcryptHash);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Value).IsEqualTo(validBcryptHash);
    }

    [Test]
    public async Task Create_Should_ReturnError_When_HashIsInvalidFormat()
    {
        // Arrange
        const string invalidHash = "not a hash format at all !";

        // Act
        var result = PasswordHash.Create(invalidHash);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task FromTrusted_Should_CreatePasswordHash_When_Called()
    {
        // Arrange
        const string trustedHash = "any_trusted_string";

        // Act
        var result = PasswordHash.FromTrusted(trustedHash);

        // Assert
        await Assert.That(result.Value).IsEqualTo(trustedHash);
    }
}
