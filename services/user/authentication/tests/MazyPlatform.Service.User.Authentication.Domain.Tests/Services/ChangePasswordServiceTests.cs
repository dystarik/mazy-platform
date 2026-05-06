namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class ChangePasswordServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_AccountNotFound()
    {
        // Arrange
        var (service, repo, _, _, _) = CreateService();
        var userAccountId = Guid.NewGuid();
        var currentPassword = Password.Create("OldPass1!").Value!;
        var newPassword = Password.Create("NewPass1!").Value!;
        repo.GetByIdAsync(userAccountId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(userAccountId, currentPassword, newPassword, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_CurrentPasswordWrong()
    {
        // Arrange
        var (service, repo, _, hasher, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);
        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var currentPassword = Password.Create("WrongPass1!").Value!;
        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, currentPassword, newPassword, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnMfaRequired_When_AccountHasMfaAndNoSession()
    {
        // Arrange
        var (service, repo, _, hasher, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);

        var backupHasher = Substitute.For<IBackupCodeHasher>();
        backupHasher.Hash(Arg.Any<string>()).Returns("hashed");
        account.AddMfaMethod(new EmailPayload(email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, backupHasher, Now);

        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var currentPassword = Password.Create("OldPass1!").Value!;
        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, currentPassword, newPassword, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsTrue();
        await Assert.That(result.Value.MfaRequiredData).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_NoMfaAndCorrectPassword()
    {
        // Arrange
        var (service, repo, _, hasher, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);
        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        hasher.Hash(Arg.Any<string>()).Returns("new_hashed");

        var currentPassword = Password.Create("OldPass1!").Value!;
        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, currentPassword, newPassword, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsFalse();
        await Assert.That(account.PasswordHash!.Value).IsEqualTo("new_hashed");
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_MfaSessionValid()
    {
        // Arrange
        var (service, repo, mfaRepo, hasher, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);

        var backupHasher = Substitute.For<IBackupCodeHasher>();
        backupHasher.Hash(Arg.Any<string>()).Returns("hashed");
        account.AddMfaMethod(new EmailPayload(email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, backupHasher, Now);

        var mfaSession = MfaSession.Create(account.Id, MfaSessionAction.ChangePassword, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);

        repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, account.Id, Arg.Any<CancellationToken>())
            .Returns(mfaSession);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        hasher.Hash(Arg.Any<string>()).Returns("new_hashed");

        var currentPassword = Password.Create("OldPass1!").Value!;
        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(account.Id, currentPassword, newPassword, mfaSession.Id, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsFalse();
    }

    private static (ChangePasswordService Service, IUserAccountRepository Repo, IMfaSessionRepository MfaRepo, IPasswordHasher Hasher, TimeProvider Time)
        CreateService()
    {
        var repo = Substitute.For<IUserAccountRepository>();
        var mfaRepo = Substitute.For<IMfaSessionRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        return (new ChangePasswordService(repo, mfaRepo, hasher, time), repo, mfaRepo, hasher, time);
    }
}
