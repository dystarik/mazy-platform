namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserAccounts;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class UserAccountTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    // ── RegisterByPassword ────────────────────────────────────────────────────
    [Test]
    public async Task RegisterByPassword_Should_CreateAccount_When_ValidParameters()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var passwordHash = PasswordHash.FromTrusted("hashed_password");

        // Act
        var account = UserAccount.RegisterByPassword(email, passwordHash, Now);

        // Assert
        await Assert.That(account.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(account.Email).IsEqualTo(email);
        await Assert.That(account.PasswordHash).IsEqualTo(passwordHash);
        await Assert.That(account.PasswordSetAt).IsEqualTo(Now);
        await Assert.That(account.CreatedAt).IsEqualTo(Now);
        await Assert.That(account.IsEmailVerified).IsFalse();
        await Assert.That(account.HasPassword).IsTrue();
        await Assert.That(account.MfaSettings).IsNotNull();
        await Assert.That(account.UserLinkedProviders).IsNotNull();
        await Assert.That(account.MfaSettings.HasConfirmedMfaMethod).IsFalse();
        await Assert.That(account.UserLinkedProviders.LinkedProviders).IsEmpty();
    }

    [Test]
    public async Task RegisterByExternalProvider_Should_CreateAccountAndLinkProvider_When_ValidParameters()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);

        // Act
        var account = UserAccount.RegisterByExternalProvider(email, provider, Now);

        // Assert
        await Assert.That(account.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(account.Email).IsEqualTo(email);
        await Assert.That(account.CreatedAt).IsEqualTo(Now);
        await Assert.That(account.HasPassword).IsFalse();
        await Assert.That(account.PasswordHash).IsNull();
        await Assert.That(account.IsEmailVerified).IsFalse();
        await Assert.That(account.UserLinkedProviders.LinkedProviders.Count).IsEqualTo(1);
        await Assert.That(account.UserLinkedProviders.LinkedProviders.First()).IsEqualTo(provider);
    }

    // ── ChangePassword ────────────────────────────────────────────────────────
    [Test]
    public async Task ChangePassword_Should_UpdatePasswordHash_When_Called()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var oldHash = PasswordHash.FromTrusted("old_hash");
        var account = UserAccount.RegisterByPassword(email, oldHash, Now);
        var newHash = PasswordHash.FromTrusted("new_hash");
        var changeTime = Now.AddHours(1);

        // Act
        var result = account.ChangePassword(newHash, changeTime);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(account.PasswordHash).IsEqualTo(newHash);
        await Assert.That(account.PasswordSetAt).IsEqualTo(changeTime);
    }

    // ── AddMfaMethod ──────────────────────────────────────────────────────────
    [Test]
    public async Task AddMfaMethod_Should_ReturnTrue_When_TotpPayloadAdded()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        var payload = new TotpPayload("JBSWY3DPEHPK3PXP");

        // Act
        var result = account.AddMfaMethod(payload, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsTrue(); // TOTP always requires confirmation
        await Assert.That(account.MfaSettings.MfaMethods.Count).IsEqualTo(1);
        await Assert.That(account.MfaSettings.MfaMethods.First().IsConfirmed).IsFalse();
    }

    [Test]
    public async Task AddMfaMethod_Should_ReturnFalse_When_EmailPayloadMatchesAccountEmail()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        var payload = new EmailPayload(email); // same email — no confirmation needed

        // Act
        var result = account.AddMfaMethod(payload, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsFalse(); // no confirmation required
    }

    [Test]
    public async Task AddMfaMethod_Should_ReturnTrue_When_EmailPayloadDiffersFromAccountEmail()
    {
        // Arrange
        var accountEmail = Email.Create("test@example.com").Value!;
        var otherEmail = Email.Create("other@example.com").Value!;
        var account = UserAccount.RegisterByPassword(accountEmail, PasswordHash.FromTrusted("hash"), Now);
        var payload = new EmailPayload(otherEmail);

        // Act
        var result = account.AddMfaMethod(payload, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsTrue(); // different email — needs confirmation
    }

    [Test]
    public async Task AddMfaMethod_Should_ReturnError_When_ConfirmedMethodAlreadyExists()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        var hasher = CreateHasher();
        account.AddMfaMethod(new EmailPayload(email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);

        // Act
        var result = account.AddMfaMethod(new EmailPayload(email), Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.MfaMethodAlreadyExists);
    }

    // ── ConfirmMfaMethod ──────────────────────────────────────────────────────
    [Test]
    public async Task ConfirmMfaMethod_Should_ReturnBackupCodes_When_FirstMethodConfirmed()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.AddMfaMethod(new EmailPayload(email), Now);
        var hasher = CreateHasher();

        // Act
        var result = account.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(10);
        await Assert.That(account.MfaSettings.HasConfirmedMfaMethod).IsTrue();
        await Assert.That(account.MfaSettings.BackupCodes.Count).IsEqualTo(10);
    }

    [Test]
    public async Task ConfirmMfaMethod_Should_ReturnEmptyBackupCodes_When_SecondMethodConfirmed()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        var hasher = CreateHasher();
        account.AddMfaMethod(new EmailPayload(email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);
        account.AddMfaMethod(new TotpPayload("JBSWY3DPEHPK3PXP"), Now);

        // Act
        var result = account.ConfirmMfaMethod(MfaMethodType.Totp, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEmpty(); // no new backup codes for second method
    }

    [Test]
    public async Task ConfirmMfaMethod_Should_ReturnError_When_MethodNotFound()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        var hasher = CreateHasher();

        // Act
        var result = account.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.MfaMethodNotFound);
    }

    // ── RemoveMfaMethod ───────────────────────────────────────────────────────
    [Test]
    public async Task RemoveMfaMethod_Should_ReturnSuccess_And_RemoveMethod_When_MethodExists()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.AddMfaMethod(new EmailPayload(email), Now);

        // Act
        var result = account.RemoveMfaMethod(MfaMethodType.Email, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(account.MfaSettings.MfaMethods).IsEmpty();
    }

    [Test]
    public async Task RemoveMfaMethod_Should_ReturnError_When_MethodNotFound()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);

        // Act
        var result = account.RemoveMfaMethod(MfaMethodType.Email, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.MfaMethodNotFound);
    }

    // ── UseBackupCode ─────────────────────────────────────────────────────────
    [Test]
    public async Task UseBackupCode_Should_ReturnSuccess_And_ConsumeCode_When_ValidCode()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.AddMfaMethod(new EmailPayload(email), Now);
        var hasher = CreateHasher();
        var confirmResult = account.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);
        var plainCode = confirmResult.Value!.First();

        // Act
        var result = account.UseBackupCode(plainCode, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(account.MfaSettings.BackupCodes.Count).IsEqualTo(9); // one consumed
    }

    [Test]
    public async Task UseBackupCode_Should_ReturnError_When_InvalidCode()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.AddMfaMethod(new EmailPayload(email), Now);
        var hasher = CreateHasher();
        account.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);
        var wrongCode = BackupCode.Create("aabbcc").Value!; // valid format but wrong value

        // Act
        var result = account.UseBackupCode(wrongCode, hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.BackupCodeInvalid);
    }

    // ── GenerateNewBackupCodes ────────────────────────────────────────────────
    [Test]
    public async Task GenerateNewBackupCodes_Should_ReturnTenCodes_When_HasConfirmedMethod()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.AddMfaMethod(new EmailPayload(email), Now);
        var hasher = CreateHasher();
        account.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);

        // Act
        var result = account.GenerateNewBackupCodes(hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(10);
        await Assert.That(account.MfaSettings.BackupCodes.Count).IsEqualTo(10);
    }

    [Test]
    public async Task GenerateNewBackupCodes_Should_ReturnError_When_NoConfirmedMethod()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        var hasher = CreateHasher();

        // Act
        var result = account.GenerateNewBackupCodes(hasher, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.NoConfirmedMfaMethod);
    }

    // ── LinkProvider ──────────────────────────────────────────────────────────
    [Test]
    public async Task LinkProvider_Should_ReturnSuccess_When_EmailMatchesAndProviderNotLinked()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);

        // Act
        var result = account.LinkProvider(provider, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(account.UserLinkedProviders.LinkedProviders.Count).IsEqualTo(1);
        await Assert.That(account.IsLinked(provider)).IsTrue();
    }

    [Test]
    public async Task LinkProvider_Should_ReturnSuccess_When_ProviderNotLinked()
    {
        // Arrange
        var accountEmail = Email.Create("test@example.com").Value!;
        var providerEmail = Email.Create("other@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, providerEmail);
        var account = UserAccount.RegisterByPassword(accountEmail, PasswordHash.FromTrusted("hash"), Now);

        // Act
        var result = account.LinkProvider(provider, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task LinkProvider_Should_ReturnError_When_ProviderAlreadyLinked()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        var account = UserAccount.RegisterByExternalProvider(email, provider, Now);

        // Act
        var result = account.LinkProvider(provider, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.ExternalProviderAlreadyLinked);
    }

    // ── UnlinkProvider ────────────────────────────────────────────────────────
    [Test]
    public async Task UnlinkProvider_Should_ReturnSuccess_When_HasPasswordAndProviderLinked()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.LinkProvider(provider, Now);

        // Act
        var result = account.UnlinkProvider(ExternalProviderType.Yandex, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(account.UserLinkedProviders.LinkedProviders).IsEmpty();
    }

    [Test]
    public async Task UnlinkProvider_Should_ReturnError_When_IsLastAuthMethodAndNoPassword()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        var account = UserAccount.RegisterByExternalProvider(email, provider, Now);

        // Act
        var result = account.UnlinkProvider(ExternalProviderType.Yandex, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code)
            .IsEqualTo(ErrorCodes.Auth.UserAccount.CannotUnlinkLastAuthenticationMethod);
    }

    [Test]
    public async Task UnlinkProvider_Should_ReturnError_When_ProviderNotLinked()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);

        // Act
        var result = account.UnlinkProvider(ExternalProviderType.Yandex, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.ExternalProviderNotLinked);
    }

    // ── IsLinked ──────────────────────────────────────────────────────────────
    [Test]
    public async Task IsLinked_Should_ReturnTrue_When_ProviderLinked()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        var account = UserAccount.RegisterByExternalProvider(email, provider, Now);

        // Act & Assert
        await Assert.That(account.IsLinked(provider)).IsTrue();
    }

    [Test]
    public async Task IsLinked_Should_ReturnFalse_When_ProviderNotLinked()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);

        // Act & Assert
        await Assert.That(account.IsLinked(provider)).IsFalse();
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
