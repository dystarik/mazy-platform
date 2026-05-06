namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при отвязке внешнего провайдера от аккаунта.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.UnlinkProvider"/>.
/// </remarks>
/// <seealso cref="UserAccountExternalProviderDomainEventBase"/>
public sealed record UserAccountExternalProviderRemovedDomainEvent : UserAccountExternalProviderDomainEventBase
{
    /// <summary>
    /// Создаёт событие отвязки внешнего провайдера.
    /// </summary>
    /// <param name="occurredAt">Временная метка UTC момента возникновения события.</param>
    /// <param name="userId">Идентификатор аккаунта.</param>
    /// <param name="email">Email аккаунта.</param>
    /// <param name="providerType">Тип отвязанного внешнего провайдера.</param>
    public UserAccountExternalProviderRemovedDomainEvent(
        DateTimeOffset occurredAt,
        Guid userId,
        Email email,
        ExternalProviderType providerType)
        : base(occurredAt, userId, email, providerType) { }
}
