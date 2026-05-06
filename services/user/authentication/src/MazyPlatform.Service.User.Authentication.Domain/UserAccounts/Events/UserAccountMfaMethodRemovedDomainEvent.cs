namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при удалении метода MFA из аккаунта.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.RemoveMfaMethod"/>.
/// Обработчики могут использовать событие для уведомления пользователя об изменении
/// настроек безопасности.
/// </remarks>
/// <seealso cref="UserAccountMfaMethodDomainEventBase"/>
public sealed record UserAccountMfaMethodRemovedDomainEvent : UserAccountMfaMethodDomainEventBase
{
    public UserAccountMfaMethodRemovedDomainEvent(
       DateTimeOffset occurredAt,
       Guid userId,
       Email email,
       MfaMethodType methodType)
    : base(occurredAt, userId, email, methodType) { }
}
