namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserAccounts.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public class BackupCodeTests
{
    // ── Create ────────────────────────────────────────────────────────────────
    [Test]
    public async Task Create_Should_ReturnSuccess_When_CodeIsExactlyLength()
    {
        // Arrange
        const string validCode = "a1b2c3"; // 6 hex chars

        // Act
        var result = BackupCode.Create(validCode);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Code).IsEqualTo(validCode);
    }

    [Test]
    public async Task Create_Should_ReturnError_When_CodeIsTooShort()
    {
        // Arrange
        const string shortCode = "abc12"; // 5 chars

        // Act
        var result = BackupCode.Create(shortCode);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.Validation.Invalid);
    }

    [Test]
    public async Task Create_Should_ReturnError_When_CodeIsTooLong()
    {
        // Arrange
        const string longCode = "a1b2c3d4"; // 8 chars

        // Act
        var result = BackupCode.Create(longCode);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.Validation.Invalid);
    }

    // ── GenerateBackupCodes ───────────────────────────────────────────────────
    [Test]
    public async Task GenerateBackupCodes_Should_ReturnRequestedCount_When_ValidCount()
    {
        // Act
        var codes = BackupCode.GenerateBackupCodes(10);

        // Assert
        await Assert.That(codes.Count()).IsEqualTo(10);
    }

    [Test]
    public async Task GenerateBackupCodes_Should_ReturnCodesOfCorrectLength_When_Generated()
    {
        // Act
        var codes = BackupCode.GenerateBackupCodes(5);

        // Assert
        foreach (var code in codes)
        {
            await Assert.That(code.Code.Length).IsEqualTo(BackupCode.Length);
        }
    }

    [Test]
    public async Task GenerateBackupCodes_Should_ReturnLowercaseHexCodes_When_Generated()
    {
        // Act
        var codes = BackupCode.GenerateBackupCodes(5);

        // Assert
        foreach (var code in codes)
        {
            await Assert.That(code.Code).Matches("^[0-9a-f]+$");
        }
    }

    [Test]
    public async Task GenerateBackupCodes_Should_ThrowArgumentException_When_CountIsZero()
    {
        // Act
        void Action() => BackupCode.GenerateBackupCodes(0);

        // Assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Test]
    public async Task GenerateBackupCodes_Should_ThrowArgumentException_When_CountIsNegative()
    {
        // Act
        void Action() => BackupCode.GenerateBackupCodes(-1);

        // Assert
        Assert.Throws<ArgumentException>(Action);
    }
}
