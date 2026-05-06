namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Totp;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class VerifyMfaFactorServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_AccountNotFound()
    {
        // Arrange
        var (service, userRepo, _, _, _, _) = CreateService();
        var session = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.Login, [MfaMethodType.Email], Now);
        userRepo.GetByIdAsync(session.UserAccountId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(session, MfaMethodType.Email, "123456", CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.InternalError);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_And_CompleteSession_When_TotpCodeValid()
    {
        // Arrange
        var (service, userRepo, _, totpService, _, _) = CreateService();
        var (account, session) = CreateAccountWithTotpMfa(Now);
        userRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        totpService.VerifyCode(Arg.Any<string>(), "123456").Returns(true);

        // Act
        var result = await service.ExecuteAsync(session, MfaMethodType.Totp, "123456", CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.IsCompleted).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_TotpCodeInvalid()
    {
        // Arrange
        var (service, userRepo, _, totpService, _, _) = CreateService();
        var (account, session) = CreateAccountWithTotpMfa(Now);
        userRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        totpService.VerifyCode(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        var result = await service.ExecuteAsync(session, MfaMethodType.Totp, "000000", CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.TotpCodeInvalid);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_And_CompleteSession_When_EmailOtpValid()
    {
        // Arrange
        var (service, userRepo, otpRepo, _, codeHasher, _) = CreateService();
        var (account, session) = CreateAccountWithEmailMfa(Now);

        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_otp");
        codeHasher.Verify(code: "123456", hashedCode: "hashed_otp").Returns(true);
        var otp = OneTimePassword.Create(account.Id, OtpType.MfaEmail, codeHasher, Now);

        userRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        otpRepo.GetLatestUnverifiedByUserAccountIdAsync(account.Id, OtpType.MfaEmail, Arg.Any<CancellationToken>())
            .Returns(otp);

        // Act
        var result = await service.ExecuteAsync(session, MfaMethodType.Email, "123456", CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.IsCompleted).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_EmailOtpNotFound()
    {
        // Arrange
        var (service, userRepo, otpRepo, _, _, _) = CreateService();
        var (account, session) = CreateAccountWithEmailMfa(Now);

        userRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        otpRepo.GetLatestUnverifiedByUserAccountIdAsync(account.Id, OtpType.MfaEmail, Arg.Any<CancellationToken>())
            .Returns((OneTimePassword?)null);

        // Act
        var result = await service.ExecuteAsync(session, MfaMethodType.Email, "123456", CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.OneTimePassword.NotFound);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_And_CompleteSession_When_BackupCodeValid()
    {
        // Arrange
        var (service, userRepo, _, _, _, backupHasher) = CreateService();
        var (account, session) = CreateAccountWithEmailMfa(Now);

        backupHasher.Hash(Arg.Any<string>()).Returns(x => "hashed_" + x.Arg<string>());
        backupHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(x =>
            string.Equals(x.ArgAt<string>(1), "hashed_" + x.ArgAt<string>(0), StringComparison.Ordinal));

        // Replace backup codes with ones the service's hasher can verify
        var newCodesResult = account.GenerateNewBackupCodes(backupHasher, Now);
        var plainCode = newCodesResult.Value!.First().Code;

        userRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        // Act
        var result = await service.ExecuteAsync(session, MfaMethodType.BackupCode, plainCode, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
    }

    private static (VerifyMfaFactorService Service, IUserAccountRepository UserRepo, IOneTimePasswordRepository OtpRepo, ITotpService TotpService, ICodeHasher CodeHasher, IBackupCodeHasher BackupHasher)
        CreateService()
    {
        var userRepo = Substitute.For<IUserAccountRepository>();
        var otpRepo = Substitute.For<IOneTimePasswordRepository>();
        var totpService = Substitute.For<ITotpService>();
        var codeHasher = Substitute.For<ICodeHasher>();
        var backupHasher = Substitute.For<IBackupCodeHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        return (new VerifyMfaFactorService(userRepo, otpRepo, totpService, codeHasher, backupHasher, time),
            userRepo, otpRepo, totpService, codeHasher, backupHasher);
    }

    private static (UserAccount Account, MfaSession Session) CreateAccountWithEmailMfa(DateTimeOffset now)
    {
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), now);
        account.VerifyEmail(now);

        var bkpHasher = Substitute.For<IBackupCodeHasher>();
        bkpHasher.Hash(Arg.Any<string>()).Returns("hashed");
        account.AddMfaMethod(new EmailPayload(email), now);
        account.ConfirmMfaMethod(MfaMethodType.Email, bkpHasher, now);

        var session = MfaSession.Create(account.Id, MfaSessionAction.Login, [MfaMethodType.Email], now);
        return (account, session);
    }

    private static (UserAccount Account, MfaSession Session) CreateAccountWithTotpMfa(DateTimeOffset now)
    {
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), now);
        account.VerifyEmail(now);

        var bkpHasher = Substitute.For<IBackupCodeHasher>();
        bkpHasher.Hash(Arg.Any<string>()).Returns("hashed");
        account.AddMfaMethod(new TotpPayload("JBSWY3DPEHPK3PXP"), now);
        account.ConfirmMfaMethod(MfaMethodType.Totp, bkpHasher, now);

        var session = MfaSession.Create(account.Id, MfaSessionAction.Login, [MfaMethodType.Totp], now);
        return (account, session);
    }
}
