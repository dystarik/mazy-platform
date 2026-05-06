namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class RegenerateBackupCodesServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ExecuteAsync_Should_ReturnTenCodes_When_ValidCompletedSession()
    {
        // Arrange
        var (service, mfaRepo, userRepo, backupHasher) = CreateService();
        var account = CreateAccountWithConfirmedMfa(backupHasher);

        // Need 2 methods available for GenerateBackupCodes action (but only 1 here → capped to 1)
        var mfaSession = MfaSession.Create(account.Id, MfaSessionAction.GenerateBackupCodes, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);

        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, account.Id, Arg.Any<CancellationToken>())
            .Returns(mfaSession);
        userRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        // Act
        var result = await service.ExecuteAsync(mfaSession.Id, account.Id, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count()).IsEqualTo(10);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_SessionNotFound()
    {
        // Arrange
        var (service, mfaRepo, _, _) = CreateService();
        var sessionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        mfaRepo.GetByIdAndUserAccountIdAsync(sessionId, accountId, Arg.Any<CancellationToken>())
            .Returns((MfaSession?)null);

        // Act
        var result = await service.ExecuteAsync(sessionId, accountId, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.MfaSession.NotFound);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_SessionHasWrongAction()
    {
        // Arrange
        var (service, mfaRepo, _, _) = CreateService();
        var accountId = Guid.NewGuid();
        var mfaSession = MfaSession.Create(accountId, MfaSessionAction.Login, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);

        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, accountId, Arg.Any<CancellationToken>())
            .Returns(mfaSession);

        // Act
        var result = await service.ExecuteAsync(mfaSession.Id, accountId, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.Validation.Invalid);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_SessionNotCompleted()
    {
        // Arrange
        var (service, mfaRepo, _, _) = CreateService();
        var accountId = Guid.NewGuid();
        var mfaSession = MfaSession.Create(accountId, MfaSessionAction.GenerateBackupCodes, [MfaMethodType.Email], Now);

        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, accountId, Arg.Any<CancellationToken>())
            .Returns(mfaSession);

        // Act
        var result = await service.ExecuteAsync(mfaSession.Id, accountId, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.MfaSession.Expired);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_SessionCompletedByBackupCode()
    {
        // Arrange
        var (service, mfaRepo, _, _) = CreateService();
        var accountId = Guid.NewGuid();
        var mfaSession = MfaSession.Create(accountId, MfaSessionAction.GenerateBackupCodes, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithBackupCode(Now);

        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, accountId, Arg.Any<CancellationToken>())
            .Returns(mfaSession);

        // Act
        var result = await service.ExecuteAsync(mfaSession.Id, accountId, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.MfaSession.PrimaryFactorRequired);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_AccountNotFound()
    {
        // Arrange
        var (service, mfaRepo, userRepo, _) = CreateService();
        var accountId = Guid.NewGuid();
        var mfaSession = MfaSession.Create(accountId, MfaSessionAction.GenerateBackupCodes, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);

        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, accountId, Arg.Any<CancellationToken>())
            .Returns(mfaSession);
        userRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(mfaSession.Id, accountId, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.InternalError);
    }

    private static (RegenerateBackupCodesService Service, IMfaSessionRepository MfaRepo, IUserAccountRepository UserRepo, IBackupCodeHasher BackupHasher)
        CreateService()
    {
        var mfaRepo = Substitute.For<IMfaSessionRepository>();
        var userRepo = Substitute.For<IUserAccountRepository>();
        var backupHasher = Substitute.For<IBackupCodeHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        backupHasher.Hash(Arg.Any<string>()).Returns(x => "hashed_" + x.Arg<string>());
        backupHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(x =>
            string.Equals(x.ArgAt<string>(1), "hashed_" + x.ArgAt<string>(0), StringComparison.Ordinal));
        return (new RegenerateBackupCodesService(mfaRepo, userRepo, backupHasher, time),
            mfaRepo, userRepo, backupHasher);
    }

    private static UserAccount CreateAccountWithConfirmedMfa(IBackupCodeHasher hasher)
    {
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);
        account.AddMfaMethod(new EmailPayload(email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, hasher, Now);
        return account;
    }
}
