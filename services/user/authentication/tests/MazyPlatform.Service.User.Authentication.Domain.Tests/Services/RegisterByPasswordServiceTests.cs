namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class RegisterByPasswordServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_EmailIsNotUsed()
    {
        // Arrange
        var (service, repo, _, _, _) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var password = Password.Create("StrongPass1!").Value!;
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(email, password, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.UserAccount).IsNotNull();
        await Assert.That(result.Value.UserAccount.Email).IsEqualTo(email);
        await Assert.That(result.Value.Otp).IsNotNull();
        await Assert.That(result.Value.Otp.UserAccountId).IsEqualTo(result.Value.UserAccount.Id);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_EmailAlreadyVerified()
    {
        // Arrange
        var (service, repo, _, _, _) = CreateService();
        var email = Email.Create("taken@example.com").Value!;
        var password = Password.Create("StrongPass1!").Value!;

        var existingAccount = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), Now);
        existingAccount.VerifyEmail(Now);
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(existingAccount);

        // Act
        var result = await service.ExecuteAsync(email, password, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.Registration.EmailAlreadyInUse);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnError_When_UnverifiedAccountIsRecent()
    {
        // Arrange
        var (service, repo, _, _, _) = CreateService();
        var email = Email.Create("pending@example.com").Value!;
        var password = Password.Create("StrongPass1!").Value!;

        // account created 1 hour ago — not yet verified, within 24h window
        var recentTime = Now.AddHours(-1);
        var existingAccount = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), recentTime);
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(existingAccount);

        // Act
        var result = await service.ExecuteAsync(email, password, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.EmailVerificationPending);
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_StaleUnverifiedAccountExists()
    {
        // Arrange
        var (service, repo, _, _, _) = CreateService();
        var email = Email.Create("stale@example.com").Value!;
        var password = Password.Create("StrongPass1!").Value!;

        // stale account — older than 24h, not verified
        var staleTime = Now.AddHours(-25);
        var staleAccount = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), staleTime);
        repo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(staleAccount);

        // Act
        var result = await service.ExecuteAsync(email, password, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        repo.Received(1).Delete(staleAccount); // stale account must be deleted
    }

    private static (RegisterByPasswordService Service, IUserAccountRepository Repo, IPasswordHasher PassHasher, ICodeHasher CodeHasher, TimeProvider Time)
        CreateService()
    {
        var repo = Substitute.For<IUserAccountRepository>();
        var passHasher = Substitute.For<IPasswordHasher>();
        var codeHasher = Substitute.For<ICodeHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        passHasher.Hash(Arg.Any<string>()).Returns("hashed_password");
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        return (new RegisterByPasswordService(repo, passHasher, time, codeHasher), repo, passHasher, codeHasher, time);
    }
}
