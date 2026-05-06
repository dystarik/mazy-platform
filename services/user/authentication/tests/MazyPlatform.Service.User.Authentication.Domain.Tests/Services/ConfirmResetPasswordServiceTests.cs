namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class ConfirmResetPasswordServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    // ── OTP path ──────────────────────────────────────────────────────────────
    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_ValidOtpCodeProvided()
    {
        // Arrange
        var (service, userRepo, otpRepo, _, codeHasher, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("old_hash"), Now);

        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_otp");
        codeHasher.Verify(code: "123456", hashedCode: "hashed_otp").Returns(true);
        var otp = OneTimePassword.Create(account.Id, OtpType.PasswordReset, codeHasher, Now);

        otpRepo.GetByIdAsync(otp.Id, Arg.Any<CancellationToken>()).Returns(otp);
        userRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, otp.Id, "123456", null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(account.PasswordHash!.Value).IsEqualTo("new_hashed_password");
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_OtpNotFound()
    {
        // Arrange
        var (service, _, otpRepo, _, _, _) = CreateService();
        var otpId = Guid.NewGuid();
        otpRepo.GetByIdAsync(otpId, Arg.Any<CancellationToken>()).Returns((OneTimePassword?)null);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, otpId, "123456", null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_OtpIsWrongType()
    {
        // Arrange
        var (service, _, otpRepo, _, codeHasher, _) = CreateService();
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_otp");

        // EmailConfirmation OTP — not PasswordReset
        var otp = OneTimePassword.Create(Guid.NewGuid(), OtpType.EmailConfirmation, codeHasher, Now);
        otpRepo.GetByIdAsync(otp.Id, Arg.Any<CancellationToken>()).Returns(otp);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, otp.Id, "123456", null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_OtpCodeInvalid()
    {
        // Arrange
        var (service, _, otpRepo, _, codeHasher, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);

        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_otp");
        codeHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var otp = OneTimePassword.Create(account.Id, OtpType.PasswordReset, codeHasher, Now);
        otpRepo.GetByIdAsync(otp.Id, Arg.Any<CancellationToken>()).Returns(otp);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, otp.Id, "wrong", null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    // ── MFA session path ──────────────────────────────────────────────────────
    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_ValidCompletedMfaSession()
    {
        // Arrange
        var (service, userRepo, _, mfaRepo, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("old_hash"), Now);

        var mfaSession = MfaSession.Create(account.Id, MfaSessionAction.ResetPassword, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);

        mfaRepo.GetByIdAsync(mfaSession.Id, Arg.Any<CancellationToken>()).Returns(mfaSession);
        userRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, null, null, mfaSession.Id, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(account.PasswordHash!.Value).IsEqualTo("new_hashed_password");
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_MfaSessionNotFound()
    {
        // Arrange
        var (service, _, _, mfaRepo, _, _) = CreateService();
        var sessionId = Guid.NewGuid();
        mfaRepo.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns((MfaSession?)null);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, null, null, sessionId, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_MfaSessionNotCompleted()
    {
        // Arrange
        var (service, _, _, mfaRepo, _, _) = CreateService();
        var mfaSession = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.ResetPassword, [MfaMethodType.Email], Now);
        mfaRepo.GetByIdAsync(mfaSession.Id, Arg.Any<CancellationToken>()).Returns(mfaSession);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, null, null, mfaSession.Id, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_MfaSessionIsWrongAction()
    {
        // Arrange
        var (service, _, _, mfaRepo, _, _) = CreateService();
        var mfaSession = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.Login, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);
        mfaRepo.GetByIdAsync(mfaSession.Id, Arg.Any<CancellationToken>()).Returns(mfaSession);

        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, null, null, mfaSession.Id, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    // ── No data path ──────────────────────────────────────────────────────────
    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_NoDataProvided()
    {
        // Arrange
        var (service, _, _, _, _, _) = CreateService();
        var newPassword = Password.Create("NewPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(newPassword, null, null, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    private static (ConfirmResetPasswordService Service, IUserAccountRepository UserRepo, IOneTimePasswordRepository OtpRepo, IMfaSessionRepository MfaRepo, ICodeHasher CodeHasher, IPasswordHasher PassHasher)
        CreateService()
    {
        var userRepo = Substitute.For<IUserAccountRepository>();
        var otpRepo = Substitute.For<IOneTimePasswordRepository>();
        var mfaRepo = Substitute.For<IMfaSessionRepository>();
        var codeHasher = Substitute.For<ICodeHasher>();
        var passHasher = Substitute.For<IPasswordHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        passHasher.Hash(Arg.Any<string>()).Returns("new_hashed_password");
        return (new ConfirmResetPasswordService(userRepo, otpRepo, mfaRepo, codeHasher, passHasher, time),
            userRepo, otpRepo, mfaRepo, codeHasher, passHasher);
    }
}
