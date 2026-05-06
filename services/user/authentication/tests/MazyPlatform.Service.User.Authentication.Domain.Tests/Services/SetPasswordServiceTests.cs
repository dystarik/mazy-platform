namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class SetPasswordServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_AccountNotFound()
    {
        // Arrange
        var (service, repo, _, _, _) = CreateService();
        var userAccountId = Guid.NewGuid();
        var newPassword = Password.Create("NewPass1!").Value!;
        repo.GetByIdAsync(userAccountId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(userAccountId, newPassword, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.NotFound);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnPasswordAlreadySet_When_AccountHasPassword()
    {
        // Arrange
        var (service, repo, _, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, newPassword, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.PasswordAlreadySet);
    }

    [Test]
    public async Task ExecuteAsync_Should_SetPassword_When_NoMfaAndAccountHasNoPassword()
    {
        // Arrange
        var (service, repo, _, hasher, _) = CreateService();
        var account = CreateExternalProviderAccount();
        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        hasher.Hash(Arg.Any<string>()).Returns("new_hashed");

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, newPassword, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsFalse();
        await Assert.That(account.PasswordHash!.Value).IsEqualTo("new_hashed");
        await Assert.That(account.PasswordSetAt).IsEqualTo(Now);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnMfaRequired_When_AccountHasMfaAndNoSession()
    {
        // Arrange
        var (service, repo, _, _, _) = CreateService();
        var account = CreateExternalProviderAccount();
        AddConfirmedEmailMfa(account);
        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, newPassword, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsTrue();
        await Assert.That(result.Value.MfaRequiredData).IsNotNull();
        await Assert.That(result.Value.MfaRequiredData!.MfaSession.Action).IsEqualTo(MfaSessionAction.SetPassword);
        await Assert.That(account.HasPassword).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_SetPassword_When_MfaSessionValid()
    {
        // Arrange
        var (service, repo, mfaRepo, hasher, _) = CreateService();
        var account = CreateExternalProviderAccount();
        AddConfirmedEmailMfa(account);

        var mfaSession = MfaSession.Create(account.Id, MfaSessionAction.SetPassword, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);

        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, account.Id, Arg.Any<CancellationToken>())
            .Returns(mfaSession);
        hasher.Hash(Arg.Any<string>()).Returns("new_hashed");

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, newPassword, mfaSession.Id, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsFalse();
        await Assert.That(account.PasswordHash!.Value).IsEqualTo("new_hashed");
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_MfaSessionInvalid()
    {
        // Arrange
        var (service, repo, mfaRepo, _, _) = CreateService();
        var account = CreateExternalProviderAccount();
        AddConfirmedEmailMfa(account);

        var mfaSession = MfaSession.Create(account.Id, MfaSessionAction.ChangePassword, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);

        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, account.Id, Arg.Any<CancellationToken>())
            .Returns(mfaSession);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, newPassword, mfaSession.Id, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.Unauthorized);
        await Assert.That(account.HasPassword).IsFalse();
    }

    private static UserAccount CreateExternalProviderAccount()
    {
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        var account = UserAccount.RegisterByExternalProvider(email, provider, Now);
        account.VerifyEmail(Now);
        return account;
    }

    private static void AddConfirmedEmailMfa(UserAccount account)
    {
        var backupHasher = Substitute.For<IBackupCodeHasher>();
        backupHasher.Hash(Arg.Any<string>()).Returns("hashed");
        account.AddMfaMethod(new EmailPayload(account.Email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, backupHasher, Now);
    }

    private static (SetPasswordService Service, IUserAccountRepository Repo, IMfaSessionRepository MfaRepo, IPasswordHasher Hasher, TimeProvider Time)
        CreateService()
    {
        var repo = Substitute.For<IUserAccountRepository>();
        var mfaRepo = Substitute.For<IMfaSessionRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        return (new SetPasswordService(repo, mfaRepo, hasher, time), repo, mfaRepo, hasher, time);
    }
}
