namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserAccounts.LinkedProviders;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

public class UserLinkedProvidersTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Create ────────────────────────────────────────────────────────────────
    [Test]
    public async Task Create_Should_ReturnEmptyProviders_When_Called()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();

        // Act
        var providers = UserLinkedProviders.Create(userAccountId, Now);

        // Assert
        await Assert.That(providers.UserAccountId).IsEqualTo(userAccountId);
        await Assert.That(providers.LinkedProviders).IsEmpty();
        await Assert.That(providers.CreatedAt).IsEqualTo(Now);
    }

    // ── LinkProvider ──────────────────────────────────────────────────────────
    [Test]
    public async Task LinkProvider_Should_ReturnSuccess_And_AddProvider_When_ProviderNotLinked()
    {
        // Arrange
        var providers = UserLinkedProviders.Create(Guid.NewGuid(), Now);
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);

        // Act
        var result = providers.LinkProvider(provider, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(providers.LinkedProviders.Count).IsEqualTo(1);
        await Assert.That(providers.LinkedProviders.First()).IsEqualTo(provider);
    }

    [Test]
    public async Task LinkProvider_Should_ReturnError_When_ProviderAlreadyLinked()
    {
        // Arrange
        var providers = UserLinkedProviders.Create(Guid.NewGuid(), Now);
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        providers.LinkProvider(provider, Now);

        // Act
        var result = providers.LinkProvider(provider, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code)
            .IsEqualTo(ErrorCodes.Auth.UserAccount.ExternalProviderAlreadyLinked);
        await Assert.That(providers.LinkedProviders.Count).IsEqualTo(1); // no duplicate
    }

    // ── UnlinkProvider ────────────────────────────────────────────────────────
    [Test]
    public async Task UnlinkProvider_Should_ReturnSuccess_And_RemoveProvider_When_ProviderLinked()
    {
        // Arrange
        var providers = UserLinkedProviders.Create(Guid.NewGuid(), Now);
        var email = Email.Create("test@example.com").Value!;
        var provider = new ExternalProvider(ExternalProviderType.Yandex, email);
        providers.LinkProvider(provider, Now);

        // Act
        var result = providers.UnlinkProvider(ExternalProviderType.Yandex, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(providers.LinkedProviders).IsEmpty();
    }

    [Test]
    public async Task UnlinkProvider_Should_ReturnError_When_ProviderNotLinked()
    {
        // Arrange
        var providers = UserLinkedProviders.Create(Guid.NewGuid(), Now);

        // Act
        var result = providers.UnlinkProvider(ExternalProviderType.Yandex, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code)
            .IsEqualTo(ErrorCodes.Auth.UserAccount.ExternalProviderNotLinked);
    }
}
