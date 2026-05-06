namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserAccounts.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class BackupCodeHashTests
{
    [Test]
    public async Task Create_Should_ReturnSuccess_When_HashIsValid()
    {
        // Arrange
        const string validHash = "a1b2c3d4e5f6"; // Hex

        // Act
        var result = BackupCodeHash.Create(validHash);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Value).IsEqualTo(validHash);
    }

    [Test]
    public async Task Create_Should_ReturnError_When_HashIsInvalid()
    {
        // Arrange
        const string invalidHash = "not_a_valid_hash_at_all___";

        // Act
        static void Action() => BackupCodeHash.Create(invalidHash);

        // Assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Test]
    public async Task FromTrusted_Should_CreateBackupCodeHash_When_Called()
    {
        // Arrange
        const string trustedHash = "some_random_value";

        // Act
        var result = BackupCodeHash.FromTrusted(trustedHash);

        // Assert
        await Assert.That(result.Value).IsEqualTo(trustedHash);
    }
}
