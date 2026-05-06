namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

public class LoginByExternalProviderServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_And_IsNewAccount_When_AccountNotFound()
    {
        // Arrange
        var (service, repo) = CreateService();
        var email = Email.Create("new@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        repo.GetByExternalProviderAsync(provider, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(provider, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.IsNewAccount).IsTrue();
        await Assert.That(result.Value.UserAccount).IsNotNull();
        await Assert.That(result.Value.UserSession).IsNotNull();
        await Assert.That(result.Value.RefreshToken).IsNotNullOrEmpty();
    }

    [Test]
    public async Task ExecuteAsync_Should_ReturnSuccess_And_IsNotNewAccount_When_ExistingAccountWithLinkedProvider()
    {
        // Arrange
        var (service, repo) = CreateService();
        var email = Email.Create("existing@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        var account = UserAccount.RegisterByExternalProvider(email, provider, Now);
        repo.GetByExternalProviderAsync(provider, Arg.Any<CancellationToken>()).Returns(account);

        // Act
        var result = await service.ExecuteAsync(provider, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.IsNewAccount).IsFalse();
        await Assert.That(result.Value.UserAccount.Id).IsEqualTo(account.Id);
    }

    [Test]
    public async Task ExecuteAsync_Should_CreateNewAccount_When_ProviderNotFound()
    {
        // Arrange
        var (service, repo) = CreateService();
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);

        repo.GetByExternalProviderAsync(provider, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        // Act
        var result = await service.ExecuteAsync(provider, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.IsNewAccount).IsTrue();
    }

    private static (LoginByExternalProviderService Service, IUserAccountRepository Repo)
        CreateService()
    {
        var repo = Substitute.For<IUserAccountRepository>();
        var tokenHasher = Substitute.For<ITokenHasher>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        tokenHasher.Hash(Arg.Any<string>()).Returns("hashed_token");
        return (new LoginByExternalProviderService(repo, tokenHasher, time), repo);
    }
}
