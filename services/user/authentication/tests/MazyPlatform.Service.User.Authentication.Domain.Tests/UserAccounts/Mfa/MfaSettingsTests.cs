namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserAccounts.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class MfaSettingsTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Create ────────────────────────────────────────────────────────────────
    [Test]
    public async Task Create_Should_ReturnEmptySettings_When_Called()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();

        // Act
        var settings = MfaSettings.Create(userAccountId, Now);

        // Assert
        await Assert.That(settings.UserAccountId).IsEqualTo(userAccountId);
        await Assert.That(settings.HasConfirmedMfaMethod).IsFalse();
        await Assert.That(settings.BackupCodes).IsEmpty();
        await Assert.That(settings.MfaMethods).IsEmpty();
        await Assert.That(settings.TotpMethod).IsNull();
        await Assert.That(settings.EmailMethod).IsNull();
    }

    // ── AddMfaMethod ──────────────────────────────────────────────────────────
    [Test]
    public async Task AddMfaMethod_Should_ReturnSuccess_When_TotpPayloadAdded()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var payload = new TotpPayload("JBSWY3DPEHPK3PXP");

        // Act
        var result = settings.AddMfaMethod(payload, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(settings.MfaMethods.Count()).IsEqualTo(1);
        await Assert.That(settings.MfaMethods.First().IsConfirmed).IsFalse();
        await Assert.That(settings.TotpMethod).IsNotNull();
    }

    [Test]
    public async Task AddMfaMethod_Should_ReturnSuccess_When_EmailPayloadAdded()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var email = Email.Create("user@example.com").Value!;
        var payload = new EmailPayload(email);

        // Act
        var result = settings.AddMfaMethod(payload, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(settings.MfaMethods.Count()).IsEqualTo(1);
        await Assert.That(settings.MfaMethods.First().IsConfirmed).IsFalse();
        await Assert.That(settings.EmailMethod).IsNotNull();
    }

    [Test]
    public async Task AddMfaMethod_Should_ReplaceUnconfirmed_When_SameTypeAdded()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var payload1 = new TotpPayload("FIRST_SECRET_KEY");
        var payload2 = new TotpPayload("SECOND_SECRET_KEY");
        settings.AddMfaMethod(payload1, Now);

        // Act
        var result = settings.AddMfaMethod(payload2, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(settings.MfaMethods.Count()).IsEqualTo(1);
        await Assert.That(settings.TotpMethod!.Secret).IsEqualTo("SECOND_SECRET_KEY");
    }

    [Test]
    public async Task AddMfaMethod_Should_ReturnError_When_ConfirmedMethodAlreadyExists()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var email = Email.Create("user@example.com").Value!;
        var hasher = CreateHasher();
        settings.AddMfaMethod(new EmailPayload(email), Now);
        settings.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);

        // Act
        var result = settings.AddMfaMethod(new EmailPayload(email), Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.MfaMethodAlreadyExists);
    }

    // ── ConfirmMfaMethod ──────────────────────────────────────────────────────
    [Test]
    public async Task ConfirmMfaMethod_Should_ReturnBackupCodes_When_FirstMethodConfirmed()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var email = Email.Create("user@example.com").Value!;
        settings.AddMfaMethod(new EmailPayload(email), Now);
        var hasher = CreateHasher();

        // Act
        var result = settings.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count()).IsEqualTo(10);
        await Assert.That(settings.HasConfirmedMfaMethod).IsTrue();
        await Assert.That(settings.BackupCodes.Count()).IsEqualTo(10);
        await Assert.That(settings.MfaMethods.First().IsConfirmed).IsTrue();
    }

    [Test]
    public async Task ConfirmMfaMethod_Should_ReturnEmpty_When_SecondMethodConfirmed()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var email = Email.Create("user@example.com").Value!;
        var hasher = CreateHasher();
        settings.AddMfaMethod(new EmailPayload(email), Now);
        settings.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);
        settings.AddMfaMethod(new TotpPayload("JBSWY3DPEHPK3PXP"), Now);

        // Act
        var result = settings.ConfirmMfaMethod(MfaMethodType.Totp, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }

    [Test]
    public async Task ConfirmMfaMethod_Should_ReturnError_When_MethodNotFound()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var hasher = CreateHasher();

        // Act
        var result = settings.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.MfaMethodNotFound);
    }

    // ── RemoveMfaMethod ───────────────────────────────────────────────────────
    [Test]
    public async Task RemoveMfaMethod_Should_ReturnSuccess_And_RemoveMethod_When_MethodExists()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var email = Email.Create("user@example.com").Value!;
        settings.AddMfaMethod(new EmailPayload(email), Now);

        // Act
        var result = settings.RemoveMfaMethod(MfaMethodType.Email, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(settings.MfaMethods).IsEmpty();
    }

    [Test]
    public async Task RemoveMfaMethod_Should_ReturnError_When_MethodNotFound()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);

        // Act
        var result = settings.RemoveMfaMethod(MfaMethodType.Totp, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.MfaMethodNotFound);
    }

    // ── UseBackupCode ─────────────────────────────────────────────────────────
    [Test]
    public async Task UseBackupCode_Should_ReturnSuccess_And_ConsumeCode_When_ValidCode()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var email = Email.Create("user@example.com").Value!;
        var hasher = CreateHasher();
        settings.AddMfaMethod(new EmailPayload(email), Now);
        var confirmResult = settings.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);
        var plainCode = confirmResult.Value!.First();

        // Act
        var result = settings.UseBackupCode(plainCode, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(settings.BackupCodes.Count()).IsEqualTo(9);
    }

    [Test]
    public async Task UseBackupCode_Should_ReturnError_When_InvalidCode()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var email = Email.Create("user@example.com").Value!;
        var hasher = CreateHasher();
        settings.AddMfaMethod(new EmailPayload(email), Now);
        settings.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);
        var wrongCode = BackupCode.Create("aabbcc").Value!;

        // Act
        var result = settings.UseBackupCode(wrongCode, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.BackupCodeInvalid);
    }

    // ── GenerateNewBackupCodes ────────────────────────────────────────────────
    [Test]
    public async Task GenerateNewBackupCodes_Should_ReturnTenCodes_When_HasConfirmedMethod()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var email = Email.Create("user@example.com").Value!;
        var hasher = CreateHasher();
        settings.AddMfaMethod(new EmailPayload(email), Now);
        settings.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);

        // Act
        var result = settings.GenerateNewBackupCodes(hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count()).IsEqualTo(10);
        await Assert.That(settings.BackupCodes.Count()).IsEqualTo(10);
    }

    [Test]
    public async Task GenerateNewBackupCodes_Should_ReturnError_When_NoConfirmedMethod()
    {
        // Arrange
        var settings = MfaSettings.Create(Guid.NewGuid(), Now);
        var hasher = CreateHasher();

        // Act
        var result = settings.GenerateNewBackupCodes(hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.NoConfirmedMfaMethod);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static IBackupCodeHasher CreateHasher()
    {
        var hasher = Substitute.For<IBackupCodeHasher>();
        hasher.Hash(Arg.Any<string>()).Returns(x => "hashed_" + x.Arg<string>());
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(x =>
            string.Equals(x.ArgAt<string>(1), "hashed_" + x.ArgAt<string>(0), StringComparison.Ordinal));
        return hasher;
    }
}
