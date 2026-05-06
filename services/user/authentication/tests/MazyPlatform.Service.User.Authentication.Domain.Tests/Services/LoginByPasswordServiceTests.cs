namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class LoginByPasswordServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_AccountNotFound()
    {
        // Arrange
        var (service, repo, _, _, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var password = Password.Create("StrongPass1!").Value!;
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(email, password, mfaSessionId: null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_EmailNotVerified()
    {
        // Arrange
        var (service, repo, _, _, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);

        // email NOT verified
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(account);

        var password = Password.Create("StrongPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(email, password, mfaSessionId: null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_PasswordWrong()
    {
        // Arrange
        var (service, repo, _, passHasher, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(account);
        passHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var password = Password.Create("WrongPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(email, password, mfaSessionId: null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnMfaRequired_When_AccountHasMfaAndNoSession()
    {
        // Arrange
        var (service, repo, _, passHasher, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);

        var backupHasher = Substitute.For<IBackupCodeHasher>();
        backupHasher.Hash(Arg.Any<string>()).Returns("hashed");
        account.AddMfaMethod(new EmailPayload(email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, backupHasher, Now);

        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(account);
        passHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var password = Password.Create("StrongPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(email, password, mfaSessionId: null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsTrue();
        await Assert.That(result.Value.MfaRequiredData).IsNotNull();
        await Assert.That(result.Value.SuccessData).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_NoMfaAndCorrectPassword()
    {
        // Arrange
        var (service, repo, _, passHasher, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(account);
        passHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var password = Password.Create("StrongPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(email, password, mfaSessionId: null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsFalse();
        await Assert.That(result.Value.SuccessData).IsNotNull();
        await Assert.That(result.Value.SuccessData!.UserSession).IsNotNull();
        await Assert.That(result.Value.SuccessData.RefreshToken).IsNotNullOrEmpty();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_ValidMfaSessionProvided()
    {
        // Arrange
        var (service, repo, mfaRepo, passHasher, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);

        var backupHasher = Substitute.For<IBackupCodeHasher>();
        backupHasher.Hash(Arg.Any<string>()).Returns("hashed");
        account.AddMfaMethod(new EmailPayload(email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, backupHasher, Now);

        var mfaSession = MfaSession.Create(account.Id, MfaSessionAction.Login, [MfaMethodType.Email], Now);
        mfaSession.CompleteWithFactor(MfaMethodType.Email, Now);

        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(account);
        mfaRepo.GetByIdAndUserAccountIdAsync(mfaSession.Id, account.Id, Arg.Any<CancellationToken>())
            .Returns(mfaSession);
        passHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var password = Password.Create("StrongPass1!").Value!;

        // Act
        var result = await service.ExecuteAsync(email, password, mfaSession.Id, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsFalse();
        await Assert.That(result.Value.SuccessData).IsNotNull();
    }

    private static (LoginByPasswordService Service, IUserAccountRepository Repo, IMfaSessionRepository MfaRepo, IPasswordHasher PassHasher, ITokenHasher TokenHasher, TimeProvider Time)
        CreateService()
    {
        var repo = Substitute.For<IUserAccountRepository>();
        var mfaRepo = Substitute.For<IMfaSessionRepository>();
        var passHasher = Substitute.For<IPasswordHasher>();
        var tokenHasher = Substitute.For<ITokenHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        tokenHasher.Hash(Arg.Any<string>()).Returns("hashed_token");
        return (new LoginByPasswordService(repo, mfaRepo, passHasher, tokenHasher, time), repo, mfaRepo, passHasher, tokenHasher, time);
    }
}
