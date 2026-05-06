namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class ResetPasswordServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_AccountNotFound()
    {
        // Arrange
        var (service, repo) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(email, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.UserAccount.NotFound);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnOtpChallenge_When_AccountHasNoMfa()
    {
        // Arrange
        var (service, repo) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(account);

        // Act
        var result = await service.ExecuteAsync(email, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsFalse();
        await Assert.That(result.Value.OtpChallengeData).IsNotNull();
        await Assert.That(result.Value.OtpChallengeData!.Otp).IsNotNull();
        await Assert.That(result.Value.MfaChallengeData).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnMfaChallenge_When_AccountHasMfa()
    {
        // Arrange
        var (service, repo) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        account.VerifyEmail(Now);

        var backupHasher = Substitute.For<IBackupCodeHasher>();
        backupHasher.Hash(Arg.Any<string>()).Returns("hashed");
        account.AddMfaMethod(new EmailPayload(email), Now);
        account.ConfirmMfaMethod(MfaMethodType.Email, backupHasher, Now);

        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(account);

        // Act
        var result = await service.ExecuteAsync(email, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.RequiresMfa).IsTrue();
        await Assert.That(result.Value.MfaChallengeData).IsNotNull();
        await Assert.That(result.Value.OtpChallengeData).IsNull();
    }

    private static (ResetPasswordService Service, IUserAccountRepository Repo)
        CreateService()
    {
        var repo = Substitute.For<IUserAccountRepository>();
        var codeHasher = Substitute.For<ICodeHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        return (new ResetPasswordService(repo, codeHasher, time), repo);
    }
}
