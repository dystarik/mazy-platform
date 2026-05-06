namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при регистрации нового аккаунта через внешний провайдер.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.RegisterByExternalProvider"/>.
/// Используется для интеграционных сценариев, где важно отличать тип регистрации.
/// </remarks>
/// <seealso cref="UserAccountExternalProviderDomainEventBase"/>
public sealed record UserAccountRegisteredByExternalProviderDomainEvent : UserAccountExternalProviderDomainEventBase
{
    internal UserAccountRegisteredByExternalProviderDomainEvent(
        DateTimeOffset occurredAt,
        Guid userId,
        Email email,
        ExternalProviderType providerType)
        : base(occurredAt, userId, email, providerType) { }
}
