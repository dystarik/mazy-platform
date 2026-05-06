namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при привязке внешнего провайдера к аккаунту.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.LinkProvider"/> и
/// косвенно из <see cref="UserAccount.RegisterByExternalProvider"/>.
/// </remarks>
/// <seealso cref="UserAccountExternalProviderDomainEventBase"/>
public sealed record UserAccountExternalProviderAddedDomainEvent : UserAccountExternalProviderDomainEventBase
{
    /// <summary>
    /// Создаёт событие привязки внешнего провайдера.
    /// </summary>
    /// <param name="occurredAt">Временная метка UTC момента возникновения события.</param>
    /// <param name="userId">Идентификатор аккаунта.</param>
    /// <param name="email">Email аккаунта.</param>
    /// <param name="providerType">Тип привязанного внешнего провайдера.</param>
    public UserAccountExternalProviderAddedDomainEvent(
        DateTimeOffset occurredAt,
        Guid userId,
        Email email,
        ExternalProviderType providerType)
        : base(occurredAt, userId, email, providerType) { }
}
